using System.ComponentModel.DataAnnotations;
using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

/// <summary>Изменение состояния оборудования на складе (например, после ремонта).</summary>
public class ConditionForm
{
    [Display(Name = "Новое состояние")]
    public EquipmentCondition? Condition { get; set; }

    [Display(Name = "Дата")]
    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }

    [Display(Name = "Комментарий")]
    public string? Comment { get; set; }
}
