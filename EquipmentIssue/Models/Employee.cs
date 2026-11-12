using System.ComponentModel.DataAnnotations;

namespace EquipmentIssue.Models;

/// <summary>Сотрудник — получатель оборудования.</summary>
public class Employee
{
    public int Id { get; set; }

    [Display(Name = "ФИО")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Табельный номер")]
    [MaxLength(20)]
    public string PersonnelNumber { get; set; } = string.Empty;

    [Display(Name = "Подразделение")]
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "Должность")]
    [MaxLength(100)]
    public string? Position { get; set; }

    /// <summary>Уволенный (неактивный) сотрудник не удаляется: на него ссылается история.</summary>
    [Display(Name = "Работает")]
    public bool IsActive { get; set; } = true;

    public List<EquipmentOperation> Operations { get; set; } = new();
}
