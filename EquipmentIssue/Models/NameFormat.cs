namespace EquipmentIssue.Models;

/// <summary>Форматирование ФИО для сообщений и таблиц.</summary>
public static class NameFormat
{
    /// <summary>«Иванов Сергей Петрович» → «Иванов С. П.»</summary>
    public static string Short(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return string.Empty;
        var initials = parts.Skip(1).Select(p => p[0] + ".");
        return string.Join(' ', new[] { parts[0] }.Concat(initials));
    }
}
