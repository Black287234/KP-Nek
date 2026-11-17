using System.ComponentModel.DataAnnotations;
using IssueDesk.Data;
using IssueDesk.Domain;
using IssueDesk.Shared;
using Microsoft.EntityFrameworkCore;

namespace IssueDesk.Features.Employees;

public class EmployeeForm
{
    [Required(ErrorMessage = "Укажите табельный номер")]
    [RegularExpression(@"^\s*\d{6}\s*$", ErrorMessage = "Табельный номер — шесть цифр")]
    public string? PersonnelNumber { get; set; }

    [Required(ErrorMessage = "Укажите фамилию")]
    [RegularExpression(@"^\s*[А-ЯЁа-яё][А-ЯЁа-яё\- ]{1,39}\s*$", ErrorMessage = "Фамилия — русскими буквами, от 2 до 40 символов")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Укажите имя")]
    [RegularExpression(@"^\s*[А-ЯЁа-яё][А-ЯЁа-яё\-]{1,39}\s*$", ErrorMessage = "Имя — русскими буквами, от 2 до 40 символов")]
    public string? FirstName { get; set; }

    [RegularExpression(@"^\s*([А-ЯЁа-яё][А-ЯЁа-яё\-]{1,39})?\s*$", ErrorMessage = "Отчество — русскими буквами")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Укажите должность")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Должность — от 3 до 80 символов")]
    public string? Position { get; set; }

    [Required(ErrorMessage = "Укажите подразделение")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Подразделение — от 3 до 80 символов")]
    public string? Department { get; set; }

    [RegularExpression(@"^[\d\s\+\-\(\)]{5,20}$", ErrorMessage = "Телефон — цифры, пробелы, скобки, «+» и «-»")]
    public string? Phone { get; set; }
}

public record EmployeeFilter(string? Text = null, string? Department = null, bool WithDismissed = false);

public class ListEmployees(IDbContextFactory<DeskDb> factory)
{
    public List<Employee> Handle(EmployeeFilter f)
    {
        using var db = factory.CreateDbContext();
        IEnumerable<Employee> q = db.Employees.AsNoTracking().ToList()
            .OrderBy(e => e.LastName, StringComparer.CurrentCulture).ThenBy(e => e.FirstName, StringComparer.CurrentCulture);
        if (!f.WithDismissed) q = q.Where(e => !e.IsDismissed);
        if (!string.IsNullOrEmpty(f.Department)) q = q.Where(e => e.Department == f.Department);
        if (Text.Clean(f.Text) is { } t)
            q = q.Where(e => e.FullName.Contains(t, StringComparison.OrdinalIgnoreCase) || e.PersonnelNumber.Contains(t));
        return q.ToList();
    }

    public List<string> Departments()
    {
        using var db = factory.CreateDbContext();
        return db.Employees.Select(e => e.Department).Distinct().ToList().Order(StringComparer.CurrentCulture).ToList();
    }
}

public class GetEmployee(IDbContextFactory<DeskDb> factory)
{
    public Employee? Handle(int id)
    {
        using var db = factory.CreateDbContext();
        return db.Employees.AsNoTracking().FirstOrDefault(e => e.Id == id);
    }
}

public class SaveEmployee(IDbContextFactory<DeskDb> factory)
{
    public Result<int> Handle(int? id, EmployeeForm form)
    {
        using var db = factory.CreateDbContext();
        var number = form.PersonnelNumber!.Trim();
        if (db.Employees.Any(e => e.PersonnelNumber == number && e.Id != id))
            return Result<int>.From(Result.Conflict($"Сотрудник с табельным номером {number} уже есть в справочнике"));

        Employee e;
        if (id is null)
        {
            e = new Employee();
            db.Employees.Add(e);
        }
        else
        {
            e = db.Employees.FirstOrDefault(x => x.Id == id)!;
            if (e is null) return Result<int>.From(Result.NotFound("Сотрудник не найден"));
        }
        e.PersonnelNumber = number;
        e.LastName = Text.Capitalize(form.LastName)!;
        e.FirstName = Text.Capitalize(form.FirstName)!;
        e.MiddleName = Text.Capitalize(form.MiddleName);
        e.Position = Text.Clean(form.Position)!;
        e.Department = Text.Clean(form.Department)!;
        e.Phone = Text.Clean(form.Phone);
        db.SaveChanges();
        return e.Id;
    }
}

/// <summary>Увольнение сотрудника (запись сохраняется для истории выдач).</summary>
public class DismissEmployee(IDbContextFactory<DeskDb> factory, TimeProvider clock)
{
    public Result Handle(int id)
    {
        using var db = factory.CreateDbContext();
        var e = db.Employees.FirstOrDefault(x => x.Id == id);
        if (e is null) return Result.NotFound("Сотрудник не найден");
        if (e.IsDismissed) return Result.Conflict("Сотрудник уже уволен");
        e.DismissedOn = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        db.SaveChanges();
        return Result.Success();
    }
}
