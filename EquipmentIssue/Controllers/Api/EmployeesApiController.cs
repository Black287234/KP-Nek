using EquipmentIssue.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers.Api;

/// <summary>API справочника сотрудников (JSON).</summary>
[ApiController]
[Route("api/employees")]
public class EmployeesApiController : ControllerBase
{
    private readonly EmployeeService _employees;

    public EmployeesApiController(EmployeeService employees) => _employees = employees;

    [HttpGet]
    public IActionResult GetAll(string? search) =>
        Ok(_employees.GetAll(true, search).Select(e => new { e.Id, e.FullName, e.PersonnelNumber, e.Department, e.Position, e.IsActive }));

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var e = _employees.GetById(id);
        if (e == null)
            return NotFound(new { error = $"Сотрудник с Id = {id} не найден" });
        return Ok(new { e.Id, e.FullName, e.PersonnelNumber, e.Department, e.Position, e.IsActive });
    }
}
