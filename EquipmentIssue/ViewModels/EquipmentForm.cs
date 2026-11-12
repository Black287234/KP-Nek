using System.ComponentModel.DataAnnotations;
using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

/// <summary>Форма добавления и изменения оборудования.</summary>
public class EquipmentForm
{
    public int Id { get; set; }

    [Display(Name = "Инвентарный номер")]
    public string? InventoryNumber { get; set; }

    [Display(Name = "Наименование")]
    public string? Name { get; set; }

    [Display(Name = "Категория")]
    public string? Category { get; set; }

    [Display(Name = "Серийный номер")]
    public string? SerialNumber { get; set; }

    /// <summary>Начальное состояние задаётся при добавлении; дальше меняется операциями.</summary>
    [Display(Name = "Состояние")]
    public EquipmentCondition Condition { get; set; } = EquipmentCondition.Good;
}
