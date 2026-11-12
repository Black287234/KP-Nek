using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

/// <summary>Фильтры истории операций.</summary>
public class HistoryFilter
{
    public int? EquipmentId { get; set; }
    public int? EmployeeId { get; set; }
    public OperationType? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
