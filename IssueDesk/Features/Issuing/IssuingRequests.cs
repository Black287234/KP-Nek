using System.ComponentModel.DataAnnotations;
using IssueDesk.Data;
using IssueDesk.Domain;
using IssueDesk.Shared;
using Microsoft.EntityFrameworkCore;

namespace IssueDesk.Features.Issuing;

public class IssueForm
{
    [Required(ErrorMessage = "Выберите оборудование")] public int? EquipmentId { get; set; }
    [Required(ErrorMessage = "Выберите сотрудника")] public int? EmployeeId { get; set; }
    [Required(ErrorMessage = "Укажите дату выдачи")] public DateOnly? IssuedOn { get; set; }
    public DateOnly? DueOn { get; set; }
    [StringLength(200, ErrorMessage = "Примечание — не более 200 символов")] public string? Note { get; set; }
}

public class ReturnForm
{
    [Required(ErrorMessage = "Выберите оборудование")] public int? EquipmentId { get; set; }
    [Required(ErrorMessage = "Укажите дату возврата")] public DateOnly? ReturnedOn { get; set; }
    public Condition Condition { get; set; } = Condition.Good;
    [StringLength(200, ErrorMessage = "Примечание — не более 200 символов")] public string? Note { get; set; }
}

/// <summary>Общие правила дат операций.</summary>
public static class Dates
{
    public const int MaxDaysBack = 30;
    public const int MaxLoanDays = 365;

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    public static string? Check(DateOnly date, DateOnly today, string what)
    {
        if (date > today) return $"Дата {what} не может быть позже сегодняшней";
        if (date < today.AddDays(-MaxDaysBack)) return $"Дата {what} — не ранее чем за {MaxDaysBack} дней до сегодняшней";
        return null;
    }
}

/// <summary>Выдача оборудования сотруднику.</summary>
public class IssueEquipment(IDbContextFactory<DeskDb> factory, TimeProvider clock)
{
    public Result<int> Handle(IssueForm f)
    {
        using var db = factory.CreateDbContext();
        var today = Dates.Today(clock);
        var item = db.Equipment.FirstOrDefault(e => e.Id == f.EquipmentId);
        if (item is null) return Fail(Result.NotFound("Оборудование не найдено"));
        var person = db.Employees.FirstOrDefault(e => e.Id == f.EmployeeId);
        if (person is null) return Fail(Result.NotFound("Сотрудник не найден"));

        if (item.IsWrittenOff) return Fail(Result.Conflict($"{item.InventoryNumber} списано и не выдаётся"));
        if (item.Condition is Condition.NeedsRepair or Condition.Broken)
            return Fail(Result.Conflict($"{item.InventoryNumber} {Names.Of(item.Condition)}: неисправное оборудование не выдаётся"));
        if (person.IsDismissed) return Fail(Result.Conflict($"Сотрудник {person.ShortName} уволен: выдача невозможна"));

        var open = db.Assignments.Include(a => a.Employee).FirstOrDefault(a => a.EquipmentId == item.Id && a.ReturnedOn == null);
        if (open != null)
            return Fail(Result.Conflict($"{item.InventoryNumber} уже выдано: находится у сотрудника {open.Employee!.ShortName} с {open.IssuedOn:dd.MM.yyyy}"));

        var issued = f.IssuedOn!.Value;
        if (Dates.Check(issued, today, "выдачи") is { } dateError) return Fail(Result.Invalid(dateError));
        if (f.DueOn is { } due)
        {
            if (due < issued) return Fail(Result.Invalid("Плановая дата возврата не может быть раньше даты выдачи"));
            if (due > issued.AddDays(Dates.MaxLoanDays)) return Fail(Result.Invalid($"Срок выдачи — не более {Dates.MaxLoanDays} дней"));
        }

        var a = new Assignment
        {
            EquipmentId = item.Id, EmployeeId = person.Id, IssuedOn = issued, IssuedCondition = item.Condition,
            DueOn = f.DueOn, IssueNote = Text.Clean(f.Note), RecordedAt = clock.GetLocalNow().DateTime
        };
        db.Assignments.Add(a);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Одновременная выдача той же единицы другим пользователем: сработал уникальный индекс
            return Fail(Result.Conflict($"{item.InventoryNumber} только что выдано другим пользователем"));
        }
        return a.Id;
    }

    private static Result<int> Fail(Result r) => Result<int>.From(r);
}

/// <summary>Возврат оборудования с фиксацией состояния.</summary>
public class ReturnEquipment(IDbContextFactory<DeskDb> factory, TimeProvider clock)
{
    public Result<int> Handle(ReturnForm f)
    {
        using var db = factory.CreateDbContext();
        var today = Dates.Today(clock);
        var a = db.Assignments.Include(x => x.Equipment).Include(x => x.Employee)
                  .FirstOrDefault(x => x.EquipmentId == f.EquipmentId && x.ReturnedOn == null);
        if (a is null) return Result<int>.From(Result.Conflict("Оборудование не числится выданным"));

        var returned = f.ReturnedOn!.Value;
        if (returned > today) return Result<int>.From(Result.Invalid("Дата возврата не может быть позже сегодняшней"));
        if (returned < a.IssuedOn)
            return Result<int>.From(Result.Invalid($"Дата возврата не может быть раньше даты выдачи ({a.IssuedOn:dd.MM.yyyy})"));
        var note = Text.Clean(f.Note);
        if (f.Condition is Condition.NeedsRepair or Condition.Broken && (note is null || note.Length < 10))
            return Result<int>.From(Result.Invalid("Опишите неисправность в примечании (не менее 10 символов)"));

        a.ReturnedOn = returned;
        a.ReturnedCondition = f.Condition;
        a.ReturnNote = note;
        a.Equipment!.Condition = f.Condition;   // состояние оборудования обновляется по результату возврата
        db.SaveChanges();
        return a.Id;
    }
}

/// <summary>Строка списка «оборудование у сотрудников».</summary>
public record Holding(Assignment Assignment, bool Overdue);

public class ListHoldings(IDbContextFactory<DeskDb> factory, TimeProvider clock)
{
    /// <summary>Незакрытые выдачи, сгруппированные по сотрудникам.</summary>
    public List<IGrouping<Employee, Holding>> Handle(string? text = null, bool overdueOnly = false)
    {
        using var db = factory.CreateDbContext();
        var today = Dates.Today(clock);
        var open = db.Assignments.AsNoTracking().Include(a => a.Equipment).Include(a => a.Employee)
                     .Where(a => a.ReturnedOn == null).ToList()
                     .Select(a => new Holding(a, a.IsOverdue(today)));
        if (overdueOnly) open = open.Where(h => h.Overdue);
        if (Text.Clean(text) is { } t)
            open = open.Where(h => h.Assignment.Employee!.FullName.Contains(t, StringComparison.OrdinalIgnoreCase)
                                || h.Assignment.Equipment!.InventoryNumber.Contains(t, StringComparison.OrdinalIgnoreCase)
                                || h.Assignment.Equipment!.Name.Contains(t, StringComparison.OrdinalIgnoreCase));
        return open.GroupBy(h => h.Assignment.Employee!, EmployeeById.Instance)
                   .OrderBy(g => g.Key.LastName, StringComparer.CurrentCulture).ThenBy(g => g.Key.FirstName, StringComparer.CurrentCulture)
                   .ToList();
    }

    private sealed class EmployeeById : IEqualityComparer<Employee>
    {
        public static readonly EmployeeById Instance = new();
        public bool Equals(Employee? x, Employee? y) => x?.Id == y?.Id;
        public int GetHashCode(Employee e) => e.Id;
    }
}

/// <summary>Событие истории операций: выдача или возврат.</summary>
public record HistoryEvent(DateOnly Date, bool IsIssue, Assignment Assignment)
{
    public Condition Condition => IsIssue ? Assignment.IssuedCondition : Assignment.ReturnedCondition!.Value;
    public string? Note => IsIssue ? Assignment.IssueNote : Assignment.ReturnNote;
}

public record HistoryFilter(DateOnly? From = null, DateOnly? To = null, int? EmployeeId = null, int? EquipmentId = null, bool? Issues = null);

/// <summary>История операций: каждая запись закрепления даёт событие выдачи и, если закрыта, событие возврата.</summary>
public class ListHistory(IDbContextFactory<DeskDb> factory)
{
    public Result<List<HistoryEvent>> Handle(HistoryFilter f)
    {
        if (f.From > f.To) return Result<List<HistoryEvent>>.From(Result.Invalid("Начало периода позже его окончания"));
        using var db = factory.CreateDbContext();
        var q = db.Assignments.AsNoTracking().Include(a => a.Equipment).Include(a => a.Employee).AsQueryable();
        if (f.EmployeeId is { } emp) q = q.Where(a => a.EmployeeId == emp);
        if (f.EquipmentId is { } eq) q = q.Where(a => a.EquipmentId == eq);
        var events = q.ToList().SelectMany(a => a.ReturnedOn is { } r
                ? new[] { new HistoryEvent(a.IssuedOn, true, a), new HistoryEvent(r, false, a) }
                : new[] { new HistoryEvent(a.IssuedOn, true, a) })
            .Where(e => (f.From == null || e.Date >= f.From) && (f.To == null || e.Date <= f.To)
                        && (f.Issues == null || e.IsIssue == f.Issues))
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.Assignment.RecordedAt).ThenBy(e => e.IsIssue)
            .ToList();
        return events;
    }
}

/// <summary>Текущий держатель оборудования.</summary>
public class CurrentHolder(IDbContextFactory<DeskDb> factory)
{
    public Assignment? Handle(int equipmentId)
    {
        using var db = factory.CreateDbContext();
        return db.Assignments.AsNoTracking().Include(a => a.Employee)
                 .FirstOrDefault(a => a.EquipmentId == equipmentId && a.ReturnedOn == null);
    }
}

/// <summary>Списки для выбора в формах выдачи и возврата.</summary>
public class IssuingChoices(IDbContextFactory<DeskDb> factory)
{
    /// <summary>Оборудование, которое можно выдать: не списано, исправно, не находится у сотрудника.</summary>
    public List<Domain.Equipment> Available()
    {
        using var db = factory.CreateDbContext();
        var busy = db.Assignments.Where(a => a.ReturnedOn == null).Select(a => a.EquipmentId).ToList();
        return db.Equipment.AsNoTracking().Where(e => e.WrittenOffOn == null && !busy.Contains(e.Id)).OrderBy(e => e.InventoryNumber).ToList()
                 .Where(e => e.Condition is Condition.Good or Condition.Worn).ToList();
    }

    public List<Assignment> Issued()
    {
        using var db = factory.CreateDbContext();
        return db.Assignments.AsNoTracking().Include(a => a.Equipment).Include(a => a.Employee)
                 .Where(a => a.ReturnedOn == null).OrderBy(a => a.Equipment!.InventoryNumber).ToList();
    }

    public List<Employee> ActiveEmployees()
    {
        using var db = factory.CreateDbContext();
        return db.Employees.AsNoTracking().Where(e => e.DismissedOn == null).ToList()
                 .OrderBy(e => e.LastName, StringComparer.CurrentCulture).ThenBy(e => e.FirstName, StringComparer.CurrentCulture).ToList();
    }
}
