using System.ComponentModel.DataAnnotations;

namespace EquipmentIssue.ViewModels;

/// <summary>Списание оборудования (вместо удаления).</summary>
public class WriteOffForm
{
    [Display(Name = "Дата списания")]
    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }

    [Display(Name = "Причина")]
    public string? Reason { get; set; }
}
