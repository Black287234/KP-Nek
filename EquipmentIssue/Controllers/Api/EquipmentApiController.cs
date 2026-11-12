using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers.Api;

/// <summary>API справочника оборудования (JSON).</summary>
[ApiController]
[Route("api/equipment")]
public class EquipmentApiController : ControllerBase
{
    private readonly EquipmentService _equipment;

    public EquipmentApiController(EquipmentService equipment) => _equipment = equipment;

    // GET /api/equipment?search=ноутбук
    [HttpGet]
    public IActionResult GetAll([FromQuery] EquipmentFilter filter) =>
        Ok(_equipment.Search(filter).Select(e => new
        {
            e.Id, e.InventoryNumber, e.Name, e.Category, e.SerialNumber,
            Condition = e.Condition.ToString(), e.IsWrittenOff
        }));

    // GET /api/equipment/5
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var e = _equipment.GetById(id);
        if (e == null)
            return NotFound(new { error = $"Оборудование с Id = {id} не найдено" });
        return Ok(new
        {
            e.Id, e.InventoryNumber, e.Name, e.Category, e.SerialNumber,
            Condition = e.Condition.ToString(), e.IsWrittenOff, e.WriteOffReason
        });
    }
}
