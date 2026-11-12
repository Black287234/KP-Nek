using EquipmentIssue.Models;

namespace EquipmentIssue.Services;

/// <summary>
/// Правила предметной области, которые не зависят от базы данных.
/// Вынесены отдельно, чтобы их можно было проверить автоматическими тестами.
/// </summary>
public static class OperationRules
{
    public const int MaxCommentLength = 200;
    public const int MinWriteOffReasonLength = 5;

    /// <summary>Проверка даты операции: указана, не в будущем, не раньше последней операции по оборудованию.</summary>
    public static string? ValidateDate(DateTime? date, DateTime today, DateTime? lastOperationDate)
    {
        if (date == null) return "Укажите дату операции";
        if (date.Value.Date > today.Date) return "Дата операции не может быть в будущем";
        if (lastOperationDate != null && date.Value.Date < lastOperationDate.Value.Date)
            return $"Дата операции не может быть раньше последней операции по этому оборудованию ({lastOperationDate:dd.MM.yyyy})";
        return null;
    }

    /// <summary>
    /// Главное правило выдачи. Выдать можно только оборудование, которое не списано,
    /// находится на складе (не выдано другому сотруднику) и не требует ремонта,
    /// и только работающему сотруднику.
    /// </summary>
    public static string? CheckIssue(Equipment equipment, Employee employee, EquipmentOperation? currentIssue)
    {
        if (equipment.IsWrittenOff)
            return $"Оборудование «{equipment.Name}» списано — выдача невозможна";
        if (currentIssue != null)
        {
            var holder = currentIssue.Employee != null ? NameFormat.Short(currentIssue.Employee.FullName) : "другого сотрудника";
            return $"Оборудование «{equipment.Name}» уже выдано: находится у {holder} с {currentIssue.Date:dd.MM.yyyy}. Сначала оформите возврат";
        }
        if (equipment.Condition == EquipmentCondition.NeedsRepair)
            return $"Оборудование «{equipment.Name}» требует ремонта — выдача запрещена до ремонта";
        if (!employee.IsActive)
            return $"Сотрудник {NameFormat.Short(employee.FullName)} уволен — выдача невозможна";
        return null;
    }

    /// <summary>Проверка возврата: оборудование должно числиться выданным, состояние указано, неисправность описана.</summary>
    public static string? CheckReturn(Equipment equipment, EquipmentOperation? currentIssue, EquipmentCondition? condition, string? comment)
    {
        if (currentIssue == null)
            return $"Оборудование «{equipment.Name}» не числится выданным — возвращать нечего";
        if (condition == null || !Enum.IsDefined(condition.Value))
            return "Укажите состояние оборудования при возврате";
        if (condition == EquipmentCondition.NeedsRepair && string.IsNullOrWhiteSpace(comment))
            return "Опишите неисправность в комментарии: оборудование возвращается в состоянии «Требует ремонта»";
        return null;
    }

    public static string? CheckComment(string? comment) =>
        comment != null && comment.Trim().Length > MaxCommentLength
            ? $"Комментарий — не длиннее {MaxCommentLength} символов"
            : null;
}
