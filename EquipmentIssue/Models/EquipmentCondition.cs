using System.ComponentModel.DataAnnotations;

namespace EquipmentIssue.Models;

/// <summary>Техническое состояние единицы оборудования.</summary>
public enum EquipmentCondition
{
    [Display(Name = "Исправно")]
    Good = 0,

    [Display(Name = "Есть износ")]
    Worn = 1,

    [Display(Name = "Требует ремонта")]
    NeedsRepair = 2
}
