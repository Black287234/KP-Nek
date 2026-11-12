using System.ComponentModel.DataAnnotations;

namespace EquipmentIssue.Models;

/// <summary>Вид операции с оборудованием.</summary>
public enum OperationType
{
    [Display(Name = "Выдача")]
    Issue = 0,

    [Display(Name = "Возврат")]
    Return = 1,

    [Display(Name = "Изменение состояния")]
    ConditionChange = 2,

    [Display(Name = "Списание")]
    WriteOff = 3
}
