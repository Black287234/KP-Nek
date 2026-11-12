using EquipmentIssue.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers;

/// <summary>Страницы справочника оборудования.</summary>
public class EquipmentController : Controller
{
    private readonly EquipmentService _equipment;

    public EquipmentController(EquipmentService equipment) => _equipment = equipment;

    // GET /Equipment — список оборудования
    public IActionResult Index() => View(_equipment.GetAll());

    // GET /Equipment/Details/5 — карточка
    public IActionResult Details(int id)
    {
        var item = _equipment.GetById(id);
        if (item == null)
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }
        return View(item);
    }
}
