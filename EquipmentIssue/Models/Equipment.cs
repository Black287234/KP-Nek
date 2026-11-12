using System.ComponentModel.DataAnnotations;

namespace EquipmentIssue.Models;

/// <summary>Единица оборудования (ноутбук, монитор, телефон, инструмент и т. п.).</summary>
public class Equipment
{
    public int Id { get; set; }

    [Display(Name = "Инвентарный номер")]
    public string InventoryNumber { get; set; } = string.Empty;

    [Display(Name = "Наименование")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Категория")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Серийный номер")]
    public string? SerialNumber { get; set; }

    [Display(Name = "Состояние")]
    public EquipmentCondition Condition { get; set; } = EquipmentCondition.Good;

    [Display(Name = "Списано")]
    public bool IsWrittenOff { get; set; }
}
