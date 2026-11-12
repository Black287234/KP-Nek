using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

/// <summary>Строка списка «Оборудование у сотрудников».</summary>
public class OnHandRow
{
    public Equipment Equipment { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public int DaysOnHand { get; set; }
    public string? Comment { get; set; }
}
