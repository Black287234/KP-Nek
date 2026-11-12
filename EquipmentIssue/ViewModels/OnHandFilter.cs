namespace EquipmentIssue.ViewModels;

/// <summary>Фильтры списка оборудования, находящегося у сотрудников.</summary>
public class OnHandFilter
{
    public int? EmployeeId { get; set; }
    public string? Department { get; set; }
    /// <summary>Поиск по наименованию или инвентарному номеру.</summary>
    public string? Search { get; set; }
}
