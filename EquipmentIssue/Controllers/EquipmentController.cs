using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers;

/// <summary>Страницы справочника оборудования.</summary>
public class EquipmentController : Controller
{
    private readonly EquipmentService _equipment;

    public EquipmentController(EquipmentService equipment) => _equipment = equipment;

    // GET /Equipment?Search=ноутбук&Category=Ноутбуки
    public IActionResult Index([FromQuery] EquipmentFilter filter)
    {
        var model = new EquipmentListViewModel
        {
            Filter = filter,
            Items = _equipment.Search(filter),
            Categories = _equipment.Categories()
        };
        return View(model);
    }

    // GET /Equipment/Details/5
    public IActionResult Details(int id)
    {
        var item = _equipment.GetById(id);
        if (item == null) return NotFoundPage();
        return View(item);
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

    private IActionResult NotFoundPage()
    {
        Response.StatusCode = 404;
        return View("NotFound");
    }
}
