using System.Diagnostics;
using EquipmentIssue.Models;
using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers;

public class HomeController : Controller
{
    private readonly EquipmentService _equipment;
    private readonly EmployeeService _employees;

    public HomeController(EquipmentService equipment, EmployeeService employees)
    {
        _equipment = equipment;
        _employees = employees;
    }

    public IActionResult Index()
    {
        ViewData["EquipmentCount"] = _equipment.Search(new EquipmentFilter()).Count;
        ViewData["EmployeeCount"] = _employees.GetAll(includeInactive: false).Count;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
