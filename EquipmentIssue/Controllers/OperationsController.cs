using EquipmentIssue.Models;
using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EquipmentIssue.Controllers;

/// <summary>Выдача и возврат оборудования, список оборудования у сотрудников, история операций.</summary>
public class OperationsController : Controller
{
    private readonly OperationService _operations;
    private readonly EquipmentService _equipment;
    private readonly EmployeeService _employees;

    public OperationsController(OperationService operations, EquipmentService equipment, EmployeeService employees)
    {
        _operations = operations;
        _equipment = equipment;
        _employees = employees;
    }

    // ---------- Выдача ----------

    // GET /Operations/Issue?equipmentId=3
    [HttpGet]
    public IActionResult Issue(int? equipmentId, int? employeeId)
    {
        FillIssueLists();
        return View(new IssueForm { EquipmentId = equipmentId, EmployeeId = employeeId, Date = _operations.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Issue(IssueForm form)
    {
        if (!ModelState.IsValid)   // в форму пришло значение, которое нельзя прочитать
        {
            FillIssueLists();
            return View(form);
        }
        var result = _operations.Issue(form);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            FillIssueLists();
            return View(form);
        }
        TempData["Message"] = result.Message;
        return RedirectToAction("Details", "Equipment", new { id = form.EquipmentId });
    }

    // ---------- Возврат ----------

    // GET /Operations/Return?equipmentId=1
    [HttpGet]
    public IActionResult Return(int? equipmentId)
    {
        FillReturnLists();
        return View(new ReturnForm { EquipmentId = equipmentId, Date = _operations.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Return(ReturnForm form)
    {
        if (!ModelState.IsValid)
        {
            FillReturnLists();
            return View(form);
        }
        var result = _operations.Return(form);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            FillReturnLists();
            return View(form);
        }
        TempData["Message"] = result.Message;
        return RedirectToAction("Details", "Equipment", new { id = form.EquipmentId });
    }

    // ---------- Списки ----------

    // GET /Operations/OnHand?EmployeeId=3 — оборудование, находящееся у сотрудников
    public IActionResult OnHand([FromQuery] OnHandFilter filter)
    {
        var employees = _employees.GetAll(includeInactive: true);
        return View(new OnHandViewModel
        {
            Filter = filter,
            Rows = _operations.GetOnHand(filter),
            Employees = employees,
            Departments = employees.Select(e => e.Department).Distinct().OrderBy(d => d).ToList()
        });
    }

    // GET /Operations?EquipmentId=1&Type=Issue&From=2026-10-01 — история операций
    public IActionResult Index([FromQuery] HistoryFilter filter)
    {
        var model = new HistoryViewModel
        {
            Filter = filter,
            AllEquipment = _equipment.Search(new EquipmentFilter { ShowWrittenOff = true }),
            AllEmployees = _employees.GetAll(includeInactive: true)
        };
        if (filter.From != null && filter.To != null && filter.From > filter.To)
            model.FilterError = "Начало периода не может быть позже окончания";
        else
            model.Items = _operations.GetHistory(filter);
        return View(model);
    }

    // ---------- Заполнение выпадающих списков ----------

    private void FillIssueLists()
    {
        // Для выдачи предлагаем только то, что можно выдать: на складе, исправное или с износом, не списанное
        var issued = _operations.CurrentIssues();
        ViewData["EquipmentList"] = _equipment.Search(new EquipmentFilter())
            .Where(e => !issued.ContainsKey(e.Id) && e.Condition != EquipmentCondition.NeedsRepair)
            .Select(e => new SelectListItem($"{e.InventoryNumber} — {e.Name} ({e.Condition.DisplayName()})", e.Id.ToString()))
            .ToList();
        ViewData["EmployeeList"] = _employees.GetAll(includeInactive: false)
            .Select(e => new SelectListItem($"{e.FullName} — {e.Department}", e.Id.ToString()))
            .ToList();
    }

    private void FillReturnLists()
    {
        var issued = _operations.CurrentIssues();
        ViewData["EquipmentList"] = _equipment.Search(new EquipmentFilter())
            .Where(e => issued.ContainsKey(e.Id))
            .Select(e => new SelectListItem(
                $"{e.InventoryNumber} — {e.Name} (у {NameFormat.Short(issued[e.Id].Employee?.FullName ?? "")} с {issued[e.Id].Date:dd.MM.yyyy})",
                e.Id.ToString()))
            .ToList();
    }
}
