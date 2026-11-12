using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers;

/// <summary>Страницы справочника сотрудников.</summary>
public class EmployeesController : Controller
{
    private readonly EmployeeService _employees;

    public EmployeesController(EmployeeService employees) => _employees = employees;

    // GET /Employees?search=иванов
    public IActionResult Index(string? search)
    {
        ViewData["Search"] = search;
        return View(_employees.GetAll(includeInactive: true, search: search));
    }

    public IActionResult Details(int id)
    {
        var e = _employees.GetById(id);
        if (e == null) return NotFoundPage();
        return View(e);
    }

    [HttpGet]
    public IActionResult Create() => View(new EmployeeForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(EmployeeForm form)
    {
        var result = _employees.Create(form);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(form);
        }
        TempData["Message"] = "Сотрудник добавлен";
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var e = _employees.GetById(id);
        if (e == null) return NotFoundPage();
        return View(new EmployeeForm
        {
            Id = e.Id,
            FullName = e.FullName,
            PersonnelNumber = e.PersonnelNumber,
            Department = e.Department,
            Position = e.Position
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, EmployeeForm form)
    {
        form.Id = id;
        var result = _employees.Update(form);
        if (result.Kind == ErrorKind.NotFound) return NotFoundPage();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(form);
        }
        TempData["Message"] = "Изменения сохранены";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST /Employees/SetActive/5?active=false — увольнение или восстановление
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetActive(int id, bool active)
    {
        var result = _employees.SetActive(id, active);
        if (result.Kind == ErrorKind.NotFound) return NotFoundPage();
        if (result.Success)
            TempData["Message"] = active ? "Сотрудник восстановлен" : "Сотрудник отмечен как уволенный";
        else
            TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    private IActionResult NotFoundPage()
    {
        Response.StatusCode = 404;
        return View("NotFound");
    }
}
