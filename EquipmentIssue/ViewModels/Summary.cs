namespace EquipmentIssue.ViewModels;

/// <summary>Сводка для главной страницы.</summary>
public class Summary
{
    public int Total { get; set; }
    public int InStock { get; set; }
    public int Issued { get; set; }
    public int NeedsRepair { get; set; }
    public int EmployeesWithEquipment { get; set; }
    public int WrittenOff { get; set; }
}
