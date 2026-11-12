using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

/// <summary>Параметры поиска и фильтрации справочника оборудования (из строки запроса).</summary>
public class EquipmentFilter
{
    /// <summary>Текст для поиска по наименованию, инвентарному и серийному номеру.</summary>
    public string? Search { get; set; }
    public string? Category { get; set; }
    public EquipmentCondition? Condition { get; set; }
    public bool ShowWrittenOff { get; set; }
}
