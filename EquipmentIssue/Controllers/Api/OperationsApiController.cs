using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentIssue.Controllers.Api;

/// <summary>API операций: выдача, возврат, оборудование у сотрудников, история (JSON).</summary>
[ApiController]
[Route("api")]
public class OperationsApiController : ControllerBase
{
    private readonly OperationService _operations;

    public OperationsApiController(OperationService operations) => _operations = operations;

    // GET /api/onhand?employeeId=3
    [HttpGet("onhand")]
    public IActionResult OnHand([FromQuery] OnHandFilter filter) =>
        Ok(_operations.GetOnHand(filter).Select(r => new
        {
            EquipmentId = r.Equipment.Id, r.Equipment.InventoryNumber, Equipment = r.Equipment.Name,
            EmployeeId = r.Employee.Id, Employee = r.Employee.FullName, r.Employee.Department,
            IssueDate = r.IssueDate.ToString("yyyy-MM-dd"), r.DaysOnHand
        }));

    // GET /api/operations?equipmentId=1&type=Issue&from=2026-10-01&to=2026-11-30
    [HttpGet("operations")]
    public IActionResult History([FromQuery] HistoryFilter filter)
    {
        if (filter.From != null && filter.To != null && filter.From > filter.To)
            return BadRequest(new { error = "Начало периода не может быть позже окончания" });
        return Ok(_operations.GetHistory(filter).Select(o => new
        {
            o.Id, Date = o.Date.ToString("yyyy-MM-dd"), o.Type,
            o.EquipmentId, Equipment = o.Equipment?.Name,
            o.EmployeeId, Employee = o.Employee?.FullName,
            o.Condition, o.Comment
        }));
    }

    // POST /api/operations/issue  { "equipmentId": 3, "employeeId": 1, "date": "2026-11-12" }
    [HttpPost("operations/issue")]
    public IActionResult Issue(IssueForm form) => ToHttp(_operations.Issue(form));

    // POST /api/operations/return { "equipmentId": 1, "date": "2026-11-12", "condition": "Good" }
    [HttpPost("operations/return")]
    public IActionResult Return(ReturnForm form) => ToHttp(_operations.Return(form));

    /// <summary>Результат сервиса → HTTP-ответ: 201, 400, 404 или 409.</summary>
    private IActionResult ToHttp(OperationResult r) => r.Kind switch
    {
        ErrorKind.None => StatusCode(201, new { id = r.CreatedId, message = r.Message }),
        ErrorKind.NotFound => NotFound(new { error = r.Error }),
        ErrorKind.Conflict => Conflict(new { error = r.Error }),
        _ => BadRequest(new { error = r.Error })
    };
}
