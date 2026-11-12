using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EquipmentIssue.Infrastructure;

/// <summary>
/// Русские сообщения об ошибках привязки данных.
/// По умолчанию ASP.NET Core выводит их по-английски («The value 'abc' is not valid…»),
/// а API отвечает стандартным объектом ProblemDetails. Исправлено на этапе 4.
/// </summary>
public static class ValidationMessages
{
    /// <summary>Сообщения для форм: неверное значение в поле, не число, пустое значение.</summary>
    public static void Configure(MvcOptions options)
    {
        var p = options.ModelBindingMessageProvider;
        p.SetAttemptedValueIsInvalidAccessor((value, field) => $"Значение «{value}» не подходит для поля «{field}»");
        p.SetUnknownValueIsInvalidAccessor(field => $"Недопустимое значение в поле «{field}»");
        p.SetValueIsInvalidAccessor(value => $"Недопустимое значение «{value}»");
        p.SetValueMustBeANumberAccessor(field => $"Поле «{field}» должно быть числом");
        p.SetValueMustNotBeNullAccessor(value => $"Значение «{value}» не может быть пустым");
        p.SetMissingBindRequiredValueAccessor(field => $"Не передано значение поля «{field}»");
        p.SetMissingKeyOrValueAccessor(() => "Не передано обязательное значение");
        p.SetMissingRequestBodyRequiredValueAccessor(() => "Тело запроса пустое");
        p.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Значение «{value}» недопустимо");
        p.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Недопустимое значение");
        p.SetNonPropertyValueMustBeANumberAccessor(() => "Значение должно быть числом");
    }

    /// <summary>
    /// Ответ API при неверных данных в запросе (неправильный JSON, дата в неверном формате):
    /// 400 и { "error": "…" } по-русски — в том же формате, что и остальные ошибки API.
    /// </summary>
    public static IActionResult ApiResponse(ActionContext context)
    {
        var fields = context.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .Select(kv => kv.Key.TrimStart('$', '.'))
            .Where(k => k.Length > 0 && k != "form")
            .Distinct()
            .ToList();

        string error = fields.Count > 0
            ? $"Неверный формат данных в запросе, проверьте поля: {string.Join(", ", fields)}. Даты передаются в виде \"ГГГГ-ММ-ДД\""
            : "Тело запроса пустое или не является JSON. Пример: {\"equipmentId\": 3, \"employeeId\": 1, \"date\": \"2026-11-12\"}";
        return new BadRequestObjectResult(new { error });
    }
}
