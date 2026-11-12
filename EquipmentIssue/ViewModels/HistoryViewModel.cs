using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

public class HistoryViewModel
{
    public HistoryFilter Filter { get; set; } = new();
    public List<EquipmentOperation> Items { get; set; } = new();
    public List<Equipment> AllEquipment { get; set; } = new();
    public List<Employee> AllEmployees { get; set; } = new();
    public string? FilterError { get; set; }
}
