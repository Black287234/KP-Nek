using System.Diagnostics;
using EquipmentIssue.Models;
using EquipmentIssue.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers;

public class HomeController : Controller
{
    private readonly OperationService _operations;

    public HomeController(OperationService operations) => _operations = operations;

    // Главная: сводка по оборудованию
    public IActionResult Index() => View(_operations.GetSummary());

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
