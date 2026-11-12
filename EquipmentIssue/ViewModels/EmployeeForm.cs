using System.ComponentModel.DataAnnotations;

namespace EquipmentIssue.ViewModels;

/// <summary>Форма добавления и изменения сотрудника.</summary>
public class EmployeeForm
{
    public int Id { get; set; }

    [Display(Name = "ФИО")]
    public string? FullName { get; set; }

    [Display(Name = "Табельный номер")]
    public string? PersonnelNumber { get; set; }

    [Display(Name = "Подразделение")]
    public string? Department { get; set; }

    [Display(Name = "Должность")]
    public string? Position { get; set; }
}
