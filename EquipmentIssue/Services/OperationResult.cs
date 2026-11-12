namespace EquipmentIssue.Services;

/// <summary>Вид ошибки — по нему API выбирает HTTP-код (400, 404, 409).</summary>
public enum ErrorKind
{
    None,
    Invalid,
    NotFound,
    Conflict
}

/// <summary>Результат операции сервиса: успех или понятное сообщение об ошибке.</summary>
public class OperationResult
{
    public ErrorKind Kind { get; private init; }
    public string? Error { get; private init; }
    public int? CreatedId { get; private init; }
    public string? Message { get; private init; }

    public bool Success => Kind == ErrorKind.None;

    public static OperationResult Ok(int? createdId = null, string? message = null) =>
        new() { Kind = ErrorKind.None, CreatedId = createdId, Message = message };

    public static OperationResult Invalid(string error) => new() { Kind = ErrorKind.Invalid, Error = error };
    public static OperationResult NotFound(string error) => new() { Kind = ErrorKind.NotFound, Error = error };
    public static OperationResult Conflict(string error) => new() { Kind = ErrorKind.Conflict, Error = error };
}
