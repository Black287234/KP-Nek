using System.ComponentModel.DataAnnotations;

namespace EquipmentIssue.ViewModels;

/// <summary>Форма выдачи оборудования сотруднику.</summary>
public class IssueForm
{
    [Display(Name = "Оборудование")]
    public int? EquipmentId { get; set; }

    [Display(Name = "Сотрудник")]
    public int? EmployeeId { get; set; }

    [Display(Name = "Дата выдачи")]
    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }

    [Display(Name = "Комментарий")]
    public string? Comment { get; set; }
}
