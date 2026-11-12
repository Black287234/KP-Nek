using System.ComponentModel.DataAnnotations;
using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

/// <summary>Форма возврата оборудования. Сотрудник определяется автоматически по последней выдаче.</summary>
public class ReturnForm
{
    [Display(Name = "Оборудование")]
    public int? EquipmentId { get; set; }

    [Display(Name = "Дата возврата")]
    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }

    [Display(Name = "Состояние при возврате")]
    public EquipmentCondition? Condition { get; set; }

    [Display(Name = "Комментарий")]
    public string? Comment { get; set; }
}
