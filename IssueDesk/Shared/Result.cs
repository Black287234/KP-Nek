namespace IssueDesk.Shared;

/// <summary>Вид отказа: запись не найдена, неверные данные, нарушено правило предметной области.</summary>
public enum Failure { None, NotFound, Invalid, Conflict }

/// <summary>
/// Результат операции. Обработчики не выбрасывают исключений при ошибках пользователя:
/// они возвращают отказ с текстом, который страница выводит без изменений.
/// </summary>
public record Result(Failure Failure, string? Error)
{
    public bool Ok => Failure == Failure.None;
    public static Result Success() => new(Failure.None, null);
    public static Result NotFound(string error) => new(Failure.NotFound, error);
    public static Result Invalid(string error) => new(Failure.Invalid, error);
    public static Result Conflict(string error) => new(Failure.Conflict, error);
}

public record Result<T>(Failure Failure, string? Error, T? Value) : Result(Failure, Error)
{
    public static Result<T> Success(T value) => new(Failure.None, null, value);
    public static implicit operator Result<T>(T value) => Success(value);
    public static Result<T> From(Result failed) => new(failed.Failure, failed.Error, default);
}
