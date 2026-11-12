using EquipmentIssue.Data;
using EquipmentIssue.Models;
using EquipmentIssue.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EquipmentIssue.Services;

/// <summary>Справочник сотрудников. Сотрудники не удаляются: на них ссылается история.</summary>
public class EmployeeService
{
    private readonly AppDbContext _db;

    public EmployeeService(AppDbContext db) => _db = db;

    public List<Employee> GetAll(bool includeInactive = true, string? search = null)
    {
        IEnumerable<Employee> items = _db.Employees.AsNoTracking().OrderBy(e => e.FullName).ToList();
        if (!includeInactive)
            items = items.Where(e => e.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim();
            items = items.Where(e => e.FullName.Contains(t, StringComparison.OrdinalIgnoreCase)
                                  || e.PersonnelNumber.Contains(t, StringComparison.OrdinalIgnoreCase)
                                  || e.Department.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
        return items.ToList();
    }

    public Employee? GetById(int id) => _db.Employees.AsNoTracking().FirstOrDefault(e => e.Id == id);

    public OperationResult Create(EmployeeForm form)
    {
        var error = Normalize(form);
        if (error != null) return OperationResult.Invalid(error);
        if (_db.Employees.Any(e => e.PersonnelNumber == form.PersonnelNumber))
            return OperationResult.Conflict($"Сотрудник с табельным номером {form.PersonnelNumber} уже есть");

        var e = new Employee
        {
            FullName = form.FullName!,
            PersonnelNumber = form.PersonnelNumber!,
            Department = form.Department!,
            Position = form.Position,
            IsActive = true
        };
        _db.Employees.Add(e);
        _db.SaveChanges();
        return OperationResult.Ok(e.Id);
    }

    public OperationResult Update(EmployeeForm form)
    {
        var e = _db.Employees.FirstOrDefault(x => x.Id == form.Id);
        if (e == null) return OperationResult.NotFound("Сотрудник не найден");

        var error = Normalize(form);
        if (error != null) return OperationResult.Invalid(error);
        if (_db.Employees.Any(x => x.PersonnelNumber == form.PersonnelNumber && x.Id != form.Id))
            return OperationResult.Conflict($"Сотрудник с табельным номером {form.PersonnelNumber} уже есть");

        e.FullName = form.FullName!;
        e.PersonnelNumber = form.PersonnelNumber!;
        e.Department = form.Department!;
        e.Position = form.Position;
        _db.SaveChanges();
        return OperationResult.Ok(e.Id);
    }

    /// <summary>Увольнение (отметка «не работает») и восстановление сотрудника.</summary>
    public OperationResult SetActive(int id, bool active)
    {
        var e = _db.Employees.FirstOrDefault(x => x.Id == id);
        if (e == null) return OperationResult.NotFound("Сотрудник не найден");
        if (e.IsActive == active)
            return OperationResult.Invalid(active ? "Сотрудник уже отмечен как работающий" : "Сотрудник уже отмечен как уволенный");

        if (!active)
        {
            // Нельзя уволить сотрудника, у которого на руках оборудование: сначала возврат
            var movements = _db.Operations.AsNoTracking()
                .Where(o => o.Type == OperationType.Issue || o.Type == OperationType.Return)
                .ToList();
            int onHand = OnHandCalculator.CurrentIssues(movements).Values.Count(o => o.EmployeeId == id);
            if (onHand > 0)
                return OperationResult.Conflict($"У сотрудника {NameFormat.Short(e.FullName)} на руках оборудование ({onHand} ед.). Сначала оформите возврат");
        }

        e.IsActive = active;
        _db.SaveChanges();
        return OperationResult.Ok(e.Id);
    }

    private static string? Normalize(EmployeeForm f)
    {
        f.FullName = string.Join(' ', (f.FullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        f.PersonnelNumber = (f.PersonnelNumber ?? "").Trim().ToUpperInvariant();
        f.Department = (f.Department ?? "").Trim();
        f.Position = string.IsNullOrWhiteSpace(f.Position) ? null : f.Position.Trim();

        if (f.FullName.Length == 0) return "Укажите ФИО";
        if (f.FullName.Length > 100) return "ФИО — не длиннее 100 символов";
        if (f.PersonnelNumber.Length == 0) return "Укажите табельный номер";
        if (f.PersonnelNumber.Length > 20) return "Табельный номер — не длиннее 20 символов";
        if (f.Department.Length == 0) return "Укажите подразделение";
        if (f.Department.Length > 100) return "Подразделение — не длиннее 100 символов";
        if (f.Position?.Length > 100) return "Должность — не длиннее 100 символов";
        return null;
    }
}
