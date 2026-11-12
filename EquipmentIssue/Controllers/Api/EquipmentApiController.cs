using EquipmentIssue.Models;
using EquipmentIssue.Services;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers.Api;

/// <summary>API справочника оборудования (JSON).</summary>
[ApiController]
[Route("api/equipment")]
public class EquipmentApiController : ControllerBase
{
    private readonly EquipmentService _equipment;

    public EquipmentApiController(EquipmentService equipment) => _equipment = equipment;

    // GET /api/equipment
    [HttpGet]
    public ActionResult<List<Equipment>> GetAll() => _equipment.GetAll();

    // GET /api/equipment/5
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var item = _equipment.GetById(id);
        if (item == null)
            return NotFound(new { error = $"Оборудование с Id = {id} не найдено" });
        return Ok(item);
    }
}
