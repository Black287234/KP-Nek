using System.Text.RegularExpressions;

namespace IssueDesk.Shared;

public static partial class Text
{
    /// <summary>Удаление пробелов по краям и сжатие повторяющихся пробелов; пустая строка — null.</summary>
    public static string? Clean(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return Spaces().Replace(s.Trim(), " ");
    }

    /// <summary>Первая буква каждого слова — заглавная: «пономарев» → «Пономарев».</summary>
    public static string? Capitalize(string? s) =>
        Clean(s) is { } c ? string.Join("-", c.Split('-').Select(p => p.Length == 0 ? p : char.ToUpper(p[0]) + p[1..].ToLower())) : null;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
