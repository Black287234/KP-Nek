using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers;

/// <summary>Страницы справочника оборудования: список, карточка, добавление, изменение, состояние, списание.</summary>
public class EquipmentController : Controller
{
    private readonly EquipmentService _equipment;
    private readonly OperationService _operations;

    public EquipmentController(EquipmentService equipment, OperationService operations)
    {
        _equipment = equipment;
        _operations = operations;
    }

    // GET /Equipment?Search=ноутбук&Location=issued
    public IActionResult Index([FromQuery] EquipmentFilter filter)
    {
        var issued = _operations.CurrentIssues();
        var model = new EquipmentListViewModel
        {
            Filter = filter,
            Items = _equipment.Search(filter, issued),
            Categories = _equipment.Categories(),
            Issued = issued
        };
        return View(model);
    }

    // GET /Equipment/Details/5 — карточка: где находится и история операций
    public IActionResult Details(int id)
    {
        var item = _equipment.GetById(id);
        if (item == null) return NotFoundPage();
        var issue = _operations.CurrentIssue(id);
        var today = _operations.Today;
        return View(new EquipmentCardViewModel
        {
            Item = item,
            CurrentIssue = issue,
            DaysOnHand = issue == null ? 0 : OnHandCalculator.DaysOnHand(issue.Date, today),
            History = _operations.GetHistory(new HistoryFilter { EquipmentId = id }),
            Today = today
        });
    }

    [HttpGet]
    public IActionResult Create() => View(new EquipmentForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(EquipmentForm form)
    {
        var result = _equipment.Create(form);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(form);
        }
        TempData["Message"] = "Оборудование добавлено в справочник";
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var item = _equipment.GetById(id);
        if (item == null) return NotFoundPage();
        return View(new EquipmentForm
        {
            Id = item.Id,
            InventoryNumber = item.InventoryNumber,
            Name = item.Name,
            Category = item.Category,
            SerialNumber = item.SerialNumber,
            Condition = item.Condition
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, EquipmentForm form)
    {
        form.Id = id;
        var result = _equipment.Update(form);
        if (result.Kind == ErrorKind.NotFound) return NotFoundPage();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(form);
        }
        TempData["Message"] = "Изменения сохранены";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Equipment/ChangeCondition/5 — например, после ремонта
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangeCondition(int id, ConditionForm form) =>
        AfterAction(id, _operations.ChangeCondition(id, form));

    // POST /Equipment/WriteOff/5 — списание вместо удаления
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult WriteOff(int id, WriteOffForm form) =>
        AfterAction(id, _operations.WriteOff(id, form));

    private IActionResult AfterAction(int id, OperationResult result)
    {
        if (result.Kind == ErrorKind.NotFound) return NotFoundPage();
        if (result.Success) TempData["Message"] = result.Message;
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult NotFoundPage()
    {
        Response.StatusCode = 404;
        return View("NotFound");
    }
}
