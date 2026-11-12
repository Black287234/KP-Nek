using EquipmentIssue.Models;

namespace EquipmentIssue.Services;

/// <summary>
/// Определяет, у кого сейчас находится оборудование, по истории операций.
/// Признак «выдано» не хранится отдельно, а вычисляется: если последняя операция
/// движения (выдача или возврат) по единице — выдача, оборудование находится у сотрудника
/// из этой выдачи; если возврат или операций нет — оборудование на складе.
/// </summary>
public static class OnHandCalculator
{
    /// <summary>
    /// Текущие выдачи: ключ — Id оборудования, значение — операция выдачи, по которой оно числится у сотрудника.
    /// </summary>
    public static Dictionary<int, EquipmentOperation> CurrentIssues(IEnumerable<EquipmentOperation> operations)
    {
        var result = new Dictionary<int, EquipmentOperation>();
        foreach (var op in operations.OrderBy(o => o.Date).ThenBy(o => o.Id))
        {
            if (op.Type == OperationType.Issue)
                result[op.EquipmentId] = op;
            else if (op.Type == OperationType.Return)
                result.Remove(op.EquipmentId);
        }
        return result;
    }

    /// <summary>Id сотрудника, у которого сейчас находится оборудование, или null (на складе).</summary>
    public static int? CurrentHolderId(IEnumerable<EquipmentOperation> operationsOfOneItem)
    {
        int? holder = null;
        foreach (var op in operationsOfOneItem.OrderBy(o => o.Date).ThenBy(o => o.Id))
        {
            if (op.Type == OperationType.Issue) holder = op.EmployeeId;
            else if (op.Type == OperationType.Return) holder = null;
        }
        return holder;
    }

    /// <summary>Сколько дней оборудование находится у сотрудника (день выдачи не считается).</summary>
    public static int DaysOnHand(DateTime issueDate, DateTime today) =>
        Math.Max(0, (today.Date - issueDate.Date).Days);
}
