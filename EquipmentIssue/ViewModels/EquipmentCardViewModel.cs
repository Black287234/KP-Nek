using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

/// <summary>Карточка оборудования: данные, где находится, история.</summary>
public class EquipmentCardViewModel
{
    public Equipment Item { get; set; } = null!;
    public EquipmentOperation? CurrentIssue { get; set; }
    public int DaysOnHand { get; set; }
    public List<EquipmentOperation> History { get; set; } = new();
    public DateTime Today { get; set; }
}
