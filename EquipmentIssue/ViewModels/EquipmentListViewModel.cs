using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

public class EquipmentListViewModel
{
    public EquipmentFilter Filter { get; set; } = new();
    public List<Equipment> Items { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    /// <summary>Текущие выдачи: Id оборудования → операция выдачи (с сотрудником).</summary>
    public Dictionary<int, EquipmentOperation> Issued { get; set; } = new();
}
