using System.ComponentModel.DataAnnotations;

namespace EquipmentIssue.Models;

/// <summary>
/// Операция с оборудованием: выдача, возврат, изменение состояния или списание.
/// Операции не изменяются и не удаляются — это журнал (история).
/// По последней выдаче/возврату определяется, у кого сейчас оборудование.
/// </summary>
public class EquipmentOperation
{
    public int Id { get; set; }

    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    /// <summary>Сотрудник: обязателен для выдачи и возврата, пуст для списания и смены состояния.</summary>
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Display(Name = "Операция")]
    public OperationType Type { get; set; }

    /// <summary>Дата операции (без времени).</summary>
    [Display(Name = "Дата")]
    public DateTime Date { get; set; }

    /// <summary>Состояние оборудования на момент операции.</summary>
    [Display(Name = "Состояние")]
    public EquipmentCondition Condition { get; set; }

    [Display(Name = "Комментарий")]
    [MaxLength(200)]
    public string? Comment { get; set; }

    /// <summary>Когда запись внесена в систему.</summary>
    public DateTime CreatedAt { get; set; }
}
