using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

/// <summary>Карточка сотрудника: оборудование на руках и история операций.</summary>
public class EmployeeCardViewModel
{
    public Employee Employee { get; set; } = null!;
    public List<OnHandRow> OnHand { get; set; } = new();
    public List<EquipmentOperation> History { get; set; } = new();
}
