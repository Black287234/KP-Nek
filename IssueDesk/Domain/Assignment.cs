namespace IssueDesk.Domain;

/// <summary>
/// Закрепление оборудования за сотрудником: одна запись на один цикл «выдача — возврат».
/// Пока ReturnedOn не заполнено, оборудование находится у сотрудника.
/// </summary>
public class Assignment
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>Дата выдачи и состояние оборудования в момент выдачи.</summary>
    public DateOnly IssuedOn { get; set; }
    public Condition IssuedCondition { get; set; }

    /// <summary>Плановая дата возврата (необязательно).</summary>
    public DateOnly? DueOn { get; set; }
    public string? IssueNote { get; set; }

    /// <summary>Дата возврата и состояние при возврате.</summary>
    public DateOnly? ReturnedOn { get; set; }
    public Condition? ReturnedCondition { get; set; }
    public string? ReturnNote { get; set; }

    public DateTime RecordedAt { get; set; }

    public bool IsOpen => ReturnedOn == null;
    public bool IsOverdue(DateOnly today) => IsOpen && DueOn is { } due && due < today;
}
