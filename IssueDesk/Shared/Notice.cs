namespace IssueDesk.Shared;

/// <summary>
/// Сообщение о результате действия, которое нужно показать после перехода на другую страницу.
/// Служба живёт в пределах соединения пользователя (scoped), поэтому сообщение не видно другим пользователям.
/// </summary>
public class Notice
{
    private string? _text;
    public void Set(string text) => _text = text;

    public string? Take()
    {
        var t = _text;
        _text = null;
        return t;
    }
}
