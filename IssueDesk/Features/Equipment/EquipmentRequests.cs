using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using IssueDesk.Data;
using IssueDesk.Domain;
using IssueDesk.Shared;
using Microsoft.EntityFrameworkCore;

namespace IssueDesk.Features.Equipment;

/// <summary>Поля формы оборудования с правилами проверки.</summary>
public class EquipmentForm
{
    [Required(ErrorMessage = "Укажите инвентарный номер")]
    [RegularExpression(@"^\s*(ИН|ин|Ин)-\d{6}\s*$", ErrorMessage = "Инвентарный номер указывается в виде ИН-000123")]
    public string? InventoryNumber { get; set; }

    [Required(ErrorMessage = "Укажите наименование")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Наименование — от 3 до 100 символов")]
    public string? Name { get; set; }

    public EquipmentKind Kind { get; set; } = EquipmentKind.Laptop;

    [StringLength(40, ErrorMessage = "Серийный номер — не более 40 символов")]
    public string? SerialNumber { get; set; }

    public Condition Condition { get; set; } = Condition.Good;
}

/// <summary>Отбор в перечне оборудования.</summary>
public record EquipmentFilter(string? Text = null, EquipmentKind? Kind = null, bool WithWrittenOff = false);

/// <summary>Перечень оборудования с отбором.</summary>
public class ListEquipment(IDbContextFactory<DeskDb> factory)
{
    public List<Domain.Equipment> Handle(EquipmentFilter f)
    {
        using var db = factory.CreateDbContext();
        IEnumerable<Domain.Equipment> q = db.Equipment.AsNoTracking().OrderBy(e => e.InventoryNumber).ToList();
        if (!f.WithWrittenOff) q = q.Where(e => !e.IsWrittenOff);
        if (f.Kind is { } k) q = q.Where(e => e.Kind == k);
        if (Text.Clean(f.Text) is { } t)
            q = q.Where(e => e.Name.Contains(t, StringComparison.OrdinalIgnoreCase)
                          || e.InventoryNumber.Contains(t, StringComparison.OrdinalIgnoreCase)
                          || (e.SerialNumber ?? "").Contains(t, StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }
}

public class GetEquipment(IDbContextFactory<DeskDb> factory)
{
    public Domain.Equipment? Handle(int id)
    {
        using var db = factory.CreateDbContext();
        return db.Equipment.AsNoTracking().FirstOrDefault(e => e.Id == id);
    }
}

/// <summary>Добавление (id = null) и изменение оборудования.</summary>
public class SaveEquipment(IDbContextFactory<DeskDb> factory)
{
    public Result<int> Handle(int? id, EquipmentForm form)
    {
        using var db = factory.CreateDbContext();
        var number = Regex.Replace(form.InventoryNumber!.Trim().ToUpperInvariant(), @"\s", "");
        if (db.Equipment.Any(e => e.InventoryNumber == number && e.Id != id))
            return Result<int>.From(Result.Conflict($"Оборудование с инвентарным номером {number} уже есть в перечне"));

        Domain.Equipment item;
        if (id is null)
        {
            item = new Domain.Equipment();
            db.Equipment.Add(item);
        }
        else
        {
            item = db.Equipment.FirstOrDefault(e => e.Id == id)!;
            if (item is null) return Result<int>.From(Result.NotFound("Оборудование не найдено"));
            if (item.IsWrittenOff) return Result<int>.From(Result.Conflict("Списанное оборудование не изменяется"));
        }
        item.InventoryNumber = number;
        item.Name = Text.Clean(form.Name)!;
        item.Kind = form.Kind;
        item.SerialNumber = Text.Clean(form.SerialNumber)?.ToUpperInvariant();
        item.Condition = form.Condition;
        db.SaveChanges();
        return item.Id;
    }
}

/// <summary>Списание оборудования с указанием причины.</summary>
public class WriteOffEquipment(IDbContextFactory<DeskDb> factory, TimeProvider clock)
{
    public Result Handle(int id, string? reason)
    {
        using var db = factory.CreateDbContext();
        var item = db.Equipment.FirstOrDefault(e => e.Id == id);
        if (item is null) return Result.NotFound("Оборудование не найдено");
        if (item.IsWrittenOff) return Result.Conflict("Оборудование уже списано");
        if (db.Assignments.Any(a => a.EquipmentId == id && a.ReturnedOn == null))
            return Result.Conflict("Оборудование находится у сотрудника: сначала оформите возврат");
        reason = Text.Clean(reason);
        if (reason is null || reason.Length < 10) return Result.Invalid("Укажите причину списания (не менее 10 символов)");
        item.WrittenOffOn = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        item.WriteOffReason = reason;
        db.SaveChanges();
        return Result.Success();
    }
}
