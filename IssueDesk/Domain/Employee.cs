namespace IssueDesk.Domain;

/// <summary>Сотрудник колледжа — получатель оборудования.</summary>
public class Employee
{
    public int Id { get; set; }

    /// <summary>Табельный номер из шести цифр; уникален.</summary>
    public string PersonnelNumber { get; set; } = "";

    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string Position { get; set; } = "";
    public string Department { get; set; } = "";
    public string? Phone { get; set; }

    /// <summary>Дата увольнения; уволенному сотруднику оборудование не выдаётся.</summary>
    public DateOnly? DismissedOn { get; set; }
    public bool IsDismissed => DismissedOn != null;

    /// <summary>Фамилия и инициалы: «Пономарев Н. С.».</summary>
    public string ShortName =>
        $"{LastName} {FirstName[..1]}.{(string.IsNullOrEmpty(MiddleName) ? "" : " " + MiddleName[..1] + ".")}";

    public string FullName => string.Join(" ", new[] { LastName, FirstName, MiddleName }.Where(s => !string.IsNullOrEmpty(s)));
}
