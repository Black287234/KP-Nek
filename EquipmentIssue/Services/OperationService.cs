using EquipmentIssue.Data;
using EquipmentIssue.Models;
using EquipmentIssue.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EquipmentIssue.Services;

/// <summary>
/// Операции с оборудованием: выдача, возврат, изменение состояния, списание;
/// список оборудования у сотрудников и история операций.
/// </summary>
public class OperationService
{
    // Проверка и запись выполняются под блокировкой, чтобы два одновременных запроса
    // не выдали одну и ту же единицу двум сотрудникам
    private static readonly object OperationLock = new();

    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public OperationService(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>Сегодняшняя дата по часам приложения (в тестах часы подменяются).</summary>
    public DateTime Today => _clock.GetLocalNow().Date;

    // ---------- Выдача ----------

    public OperationResult Issue(IssueForm form)
    {
        if (form.EquipmentId == null) return OperationResult.Invalid("Выберите оборудование");
        if (form.EmployeeId == null) return OperationResult.Invalid("Выберите сотрудника");
        var commentError = OperationRules.CheckComment(form.Comment);
        if (commentError != null) return OperationResult.Invalid(commentError);

        lock (OperationLock)
        {
            var equipment = _db.Equipment.FirstOrDefault(e => e.Id == form.EquipmentId);
            if (equipment == null) return OperationResult.NotFound("Оборудование не найдено");
            var employee = _db.Employees.FirstOrDefault(e => e.Id == form.EmployeeId);
            if (employee == null) return OperationResult.NotFound("Сотрудник не найден");

            var dateError = OperationRules.ValidateDate(form.Date, Today, LastOperationDate(equipment.Id));
            if (dateError != null) return OperationResult.Invalid(dateError);

            var ruleError = OperationRules.CheckIssue(equipment, employee, CurrentIssue(equipment.Id));
            if (ruleError != null) return OperationResult.Conflict(ruleError);

            var op = new EquipmentOperation
            {
                EquipmentId = equipment.Id,
                EmployeeId = employee.Id,
                Type = OperationType.Issue,
                Date = form.Date!.Value.Date,
                Condition = equipment.Condition,
                Comment = Clean(form.Comment),
                CreatedAt = _clock.GetLocalNow().DateTime
            };
            _db.Operations.Add(op);
            _db.SaveChanges();
            return OperationResult.Ok(op.Id,
                $"Оборудование «{equipment.Name}» ({equipment.InventoryNumber}) выдано сотруднику {NameFormat.Short(employee.FullName)}");
        }
    }

    // ---------- Возврат ----------

    public OperationResult Return(ReturnForm form)
    {
        if (form.EquipmentId == null) return OperationResult.Invalid("Выберите оборудование");
        var commentError = OperationRules.CheckComment(form.Comment);
        if (commentError != null) return OperationResult.Invalid(commentError);

        lock (OperationLock)
        {
            var equipment = _db.Equipment.FirstOrDefault(e => e.Id == form.EquipmentId);
            if (equipment == null) return OperationResult.NotFound("Оборудование не найдено");

            var issue = CurrentIssue(equipment.Id);
            if (issue == null)
                return OperationResult.Conflict($"Оборудование «{equipment.Name}» не числится выданным — возвращать нечего");

            var dateError = OperationRules.ValidateDate(form.Date, Today, LastOperationDate(equipment.Id));
            if (dateError != null) return OperationResult.Invalid(dateError);

            var ruleError = OperationRules.CheckReturn(equipment, issue, form.Condition, form.Comment);
            if (ruleError != null) return OperationResult.Invalid(ruleError);

            var condition = form.Condition!.Value;
            var op = new EquipmentOperation
            {
                EquipmentId = equipment.Id,
                EmployeeId = issue.EmployeeId,
                Type = OperationType.Return,
                Date = form.Date!.Value.Date,
                Condition = condition,
                Comment = Clean(form.Comment),
                CreatedAt = _clock.GetLocalNow().DateTime
            };
            _db.Operations.Add(op);
            equipment.Condition = condition;   // состояние оборудования обновляется по факту возврата
            _db.SaveChanges();

            var who = issue.Employee != null ? NameFormat.Short(issue.Employee.FullName) : "сотрудника";
            var message = $"Оборудование «{equipment.Name}» возвращено от {who}, состояние: {condition.DisplayName()}";
            if (condition == EquipmentCondition.NeedsRepair)
                message += ". Выдача заблокирована до ремонта";
            return OperationResult.Ok(op.Id, message);
        }
    }

    // ---------- Изменение состояния (на складе) ----------

    public OperationResult ChangeCondition(int equipmentId, ConditionForm form)
    {
        lock (OperationLock)
        {
            var equipment = _db.Equipment.FirstOrDefault(e => e.Id == equipmentId);
            if (equipment == null) return OperationResult.NotFound("Оборудование не найдено");
            if (equipment.IsWrittenOff) return OperationResult.Conflict("Оборудование списано — состояние не меняется");
            if (CurrentIssue(equipmentId) != null)
                return OperationResult.Conflict("Оборудование находится у сотрудника — состояние фиксируется при возврате");
            if (form.Condition == null || !Enum.IsDefined(form.Condition.Value))
                return OperationResult.Invalid("Выберите новое состояние");
            if (form.Condition == equipment.Condition)
                return OperationResult.Invalid("Оборудование уже в этом состоянии");
            if (string.IsNullOrWhiteSpace(form.Comment))
                return OperationResult.Invalid("Укажите причину изменения состояния в комментарии");
            var commentError = OperationRules.CheckComment(form.Comment);
            if (commentError != null) return OperationResult.Invalid(commentError);
            var dateError = OperationRules.ValidateDate(form.Date, Today, LastOperationDate(equipmentId));
            if (dateError != null) return OperationResult.Invalid(dateError);

            _db.Operations.Add(new EquipmentOperation
            {
                EquipmentId = equipmentId,
                Type = OperationType.ConditionChange,
                Date = form.Date!.Value.Date,
                Condition = form.Condition.Value,
                Comment = Clean(form.Comment),
                CreatedAt = _clock.GetLocalNow().DateTime
            });
            equipment.Condition = form.Condition.Value;
            _db.SaveChanges();
            return OperationResult.Ok(equipmentId, $"Состояние изменено: {form.Condition.Value.DisplayName()}");
        }
    }

    // ---------- Списание ----------

    public OperationResult WriteOff(int equipmentId, WriteOffForm form)
    {
        lock (OperationLock)
        {
            var equipment = _db.Equipment.FirstOrDefault(e => e.Id == equipmentId);
            if (equipment == null) return OperationResult.NotFound("Оборудование не найдено");
            if (equipment.IsWrittenOff) return OperationResult.Conflict("Оборудование уже списано");
            if (CurrentIssue(equipmentId) != null)
                return OperationResult.Conflict($"Оборудование «{equipment.Name}» находится у сотрудника. Сначала оформите возврат, затем списание");

            var reason = (form.Reason ?? "").Trim();
            if (reason.Length < OperationRules.MinWriteOffReasonLength)
                return OperationResult.Invalid($"Укажите причину списания (не короче {OperationRules.MinWriteOffReasonLength} символов)");
            if (reason.Length > OperationRules.MaxCommentLength)
                return OperationResult.Invalid($"Причина — не длиннее {OperationRules.MaxCommentLength} символов");
            var dateError = OperationRules.ValidateDate(form.Date, Today, LastOperationDate(equipmentId));
            if (dateError != null) return OperationResult.Invalid(dateError);

            _db.Operations.Add(new EquipmentOperation
            {
                EquipmentId = equipmentId,
                Type = OperationType.WriteOff,
                Date = form.Date!.Value.Date,
                Condition = equipment.Condition,
                Comment = reason,
                CreatedAt = _clock.GetLocalNow().DateTime
            });
            equipment.IsWrittenOff = true;
            equipment.WriteOffReason = reason;
            _db.SaveChanges();
            return OperationResult.Ok(equipmentId, $"Оборудование «{equipment.Name}» списано");
        }
    }

    // ---------- Чтение ----------

    /// <summary>Все текущие выдачи: Id оборудования → операция выдачи (с сотрудником).</summary>
    public Dictionary<int, EquipmentOperation> CurrentIssues() =>
        OnHandCalculator.CurrentIssues(MovementOperations(null));

    /// <summary>Текущая выдача конкретной единицы или null, если она на складе.</summary>
    public EquipmentOperation? CurrentIssue(int equipmentId) =>
        OnHandCalculator.CurrentIssues(MovementOperations(equipmentId)).GetValueOrDefault(equipmentId);

    /// <summary>Список оборудования, находящегося у сотрудников.</summary>
    public List<OnHandRow> GetOnHand(OnHandFilter filter)
    {
        var issues = CurrentIssues().Values;
        var equipment = _db.Equipment.AsNoTracking().ToDictionary(e => e.Id);
        var today = Today;

        IEnumerable<OnHandRow> rows = issues
            .Where(op => op.Employee != null && equipment.ContainsKey(op.EquipmentId))
            .Select(op => new OnHandRow
            {
                Equipment = equipment[op.EquipmentId],
                Employee = op.Employee!,
                IssueDate = op.Date,
                DaysOnHand = OnHandCalculator.DaysOnHand(op.Date, today),
                Comment = op.Comment
            });

        if (filter.EmployeeId != null)
            rows = rows.Where(r => r.Employee.Id == filter.EmployeeId);
        if (!string.IsNullOrWhiteSpace(filter.Department))
            rows = rows.Where(r => r.Employee.Department == filter.Department);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var t = filter.Search.Trim();
            rows = rows.Where(r => r.Equipment.Name.Contains(t, StringComparison.OrdinalIgnoreCase)
                                || r.Equipment.InventoryNumber.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
        return rows.OrderBy(r => r.Employee.FullName).ThenBy(r => r.Equipment.InventoryNumber).ToList();
    }

    /// <summary>История операций с фильтрами, новые сверху.</summary>
    public List<EquipmentOperation> GetHistory(HistoryFilter filter)
    {
        var q = _db.Operations.AsNoTracking()
            .Include(o => o.Equipment)
            .Include(o => o.Employee)
            .AsQueryable();

        if (filter.EquipmentId != null) q = q.Where(o => o.EquipmentId == filter.EquipmentId);
        if (filter.EmployeeId != null) q = q.Where(o => o.EmployeeId == filter.EmployeeId);
        if (filter.Type != null) q = q.Where(o => o.Type == filter.Type);
        if (filter.From != null)
        {
            var from = filter.From.Value.Date;
            q = q.Where(o => o.Date >= from);
        }
        if (filter.To != null)
        {
            var to = filter.To.Value.Date;
            q = q.Where(o => o.Date <= to);
        }
        return q.OrderByDescending(o => o.Date).ThenByDescending(o => o.Id).ToList();
    }

    public Summary GetSummary()
    {
        var all = _db.Equipment.AsNoTracking().ToList();
        var issues = CurrentIssues();
        var active = all.Where(e => !e.IsWrittenOff).ToList();
        return new Summary
        {
            Total = active.Count,
            Issued = active.Count(e => issues.ContainsKey(e.Id)),
            InStock = active.Count(e => !issues.ContainsKey(e.Id)),
            NeedsRepair = active.Count(e => e.Condition == EquipmentCondition.NeedsRepair),
            EmployeesWithEquipment = issues.Values.Select(i => i.EmployeeId).Distinct().Count(),
            WrittenOff = all.Count(e => e.IsWrittenOff)
        };
    }

    // ---------- Вспомогательное ----------

    private List<EquipmentOperation> MovementOperations(int? equipmentId)
    {
        var q = _db.Operations.AsNoTracking()
            .Include(o => o.Employee)
            .Where(o => o.Type == OperationType.Issue || o.Type == OperationType.Return);
        if (equipmentId != null) q = q.Where(o => o.EquipmentId == equipmentId);
        return q.OrderBy(o => o.Date).ThenBy(o => o.Id).ToList();
    }

    private DateTime? LastOperationDate(int equipmentId) =>
        _db.Operations.Where(o => o.EquipmentId == equipmentId)
            .OrderByDescending(o => o.Date)
            .Select(o => (DateTime?)o.Date)
            .FirstOrDefault();

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
