using EquipmentIssue.Models;

namespace EquipmentIssue.ViewModels;

public class OnHandViewModel
{
    public OnHandFilter Filter { get; set; } = new();
    public List<OnHandRow> Rows { get; set; } = new();
    public List<Employee> Employees { get; set; } = new();
    public List<string> Departments { get; set; } = new();
}
