using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

public class EquipmentListViewModel
{
    public EquipmentFilter Filter { get; set; } = new();
    public List<Equipment> Items { get; set; } = new();
    public List<string> Categories { get; set; } = new();
}
