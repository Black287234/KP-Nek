using System.ComponentModel.DataAnnotations;

namespace EquipmentIssue.Models;

/// <summary>Единица оборудования (ноутбук, монитор, телефон, инструмент и т. п.).</summary>
public class Equipment
{
    public int Id { get; set; }

    /// <summary>Инвентарный номер — уникален, хранится в верхнем регистре.</summary>
    [Display(Name = "Инвентарный номер")]
    [MaxLength(20)]
    public string InventoryNumber { get; set; } = string.Empty;

    [Display(Name = "Наименование")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Категория")]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Серийный номер")]
    [MaxLength(50)]
    public string? SerialNumber { get; set; }

    /// <summary>Текущее техническое состояние (меняется при возврате и после ремонта).</summary>
    [Display(Name = "Состояние")]
    public EquipmentCondition Condition { get; set; } = EquipmentCondition.Good;

    /// <summary>Списанное оборудование не удаляется, а остаётся в архиве ради истории.</summary>
    [Display(Name = "Списано")]
    public bool IsWrittenOff { get; set; }

    [Display(Name = "Причина списания")]
    [MaxLength(200)]
    public string? WriteOffReason { get; set; }

    /// <summary>Все операции по этой единице (выдачи, возвраты, изменения состояния, списание).</summary>
    public List<EquipmentOperation> Operations { get; set; } = new();
}
