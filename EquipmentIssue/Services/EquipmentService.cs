using EquipmentIssue.Data;
using EquipmentIssue.Models;
using EquipmentIssue.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EquipmentIssue.Services;

/// <summary>Справочник оборудования: поиск, добавление, изменение.</summary>
public class EquipmentService
{
    private readonly AppDbContext _db;

    public EquipmentService(AppDbContext db) => _db = db;

    /// <summary>
    /// Поиск и фильтры. Поиск выполняется в памяти: в SQLite сравнение
    /// без учёта регистра работает только для латиницы, а названия у нас русские.
    /// </summary>
    /// <param name="issued">Текущие выдачи (Id оборудования → выдача) — нужны для фильтра по местонахождению.</param>
    public List<Equipment> Search(EquipmentFilter filter, IReadOnlyDictionary<int, EquipmentOperation>? issued = null)
    {
        IEnumerable<Equipment> items = _db.Equipment.AsNoTracking().OrderBy(e => e.InventoryNumber).ToList();

        if (!filter.ShowWrittenOff)
            items = items.Where(e => !e.IsWrittenOff);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var t = filter.Search.Trim();
            items = items.Where(e => e.Name.Contains(t, StringComparison.OrdinalIgnoreCase)
                                  || e.InventoryNumber.Contains(t, StringComparison.OrdinalIgnoreCase)
                                  || (e.SerialNumber ?? "").Contains(t, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(filter.Category))
            items = items.Where(e => e.Category == filter.Category);
        if (filter.Condition != null)
            items = items.Where(e => e.Condition == filter.Condition);
        if (issued != null && filter.Location == "issued")
            items = items.Where(e => issued.ContainsKey(e.Id));
        if (issued != null && filter.Location == "stock")
            items = items.Where(e => !e.IsWrittenOff && !issued.ContainsKey(e.Id));

        return items.ToList();
    }

    public List<string> Categories() =>
        _db.Equipment.Select(e => e.Category).Distinct().OrderBy(c => c).ToList();

    public Equipment? GetById(int id) => _db.Equipment.AsNoTracking().FirstOrDefault(e => e.Id == id);

    public OperationResult Create(EquipmentForm form)
    {
        var error = Normalize(form);
        if (error != null) return OperationResult.Invalid(error);
        if (_db.Equipment.Any(e => e.InventoryNumber == form.InventoryNumber))
            return OperationResult.Conflict($"Оборудование с инвентарным номером {form.InventoryNumber} уже есть в справочнике");

        var item = new Equipment
        {
            InventoryNumber = form.InventoryNumber!,
            Name = form.Name!,
            Category = form.Category!,
            SerialNumber = form.SerialNumber,
            Condition = form.Condition
        };
        _db.Equipment.Add(item);
        _db.SaveChanges();
        return OperationResult.Ok(item.Id);
    }

    /// <summary>Изменение описательных полей. Состояние меняется только операциями.</summary>
    public OperationResult Update(EquipmentForm form)
    {
        var item = _db.Equipment.FirstOrDefault(e => e.Id == form.Id);
        if (item == null) return OperationResult.NotFound("Оборудование не найдено");
        if (item.IsWrittenOff) return OperationResult.Conflict("Списанное оборудование изменять нельзя");

        var error = Normalize(form);
        if (error != null) return OperationResult.Invalid(error);
        if (_db.Equipment.Any(e => e.InventoryNumber == form.InventoryNumber && e.Id != form.Id))
            return OperationResult.Conflict($"Оборудование с инвентарным номером {form.InventoryNumber} уже есть в справочнике");

        item.InventoryNumber = form.InventoryNumber!;
        item.Name = form.Name!;
        item.Category = form.Category!;
        item.SerialNumber = form.SerialNumber;
        _db.SaveChanges();
        return OperationResult.Ok(item.Id);
    }

    /// <summary>Приводит поля к единому виду и проверяет их. Возвращает текст ошибки или null.</summary>
    private static string? Normalize(EquipmentForm f)
    {
        f.InventoryNumber = (f.InventoryNumber ?? "").Trim().ToUpperInvariant();
        f.Name = (f.Name ?? "").Trim();
        f.Category = (f.Category ?? "").Trim();
        f.SerialNumber = string.IsNullOrWhiteSpace(f.SerialNumber) ? null : f.SerialNumber.Trim();

        if (f.InventoryNumber.Length == 0) return "Укажите инвентарный номер";
        if (f.InventoryNumber.Length > 20) return "Инвентарный номер — не длиннее 20 символов";
        if (f.Name.Length == 0) return "Укажите наименование";
        if (f.Name.Length > 100) return "Наименование — не длиннее 100 символов";
        if (f.Category.Length == 0) return "Укажите категорию";
        if (f.Category.Length > 50) return "Категория — не длиннее 50 символов";
        if (f.SerialNumber?.Length > 50) return "Серийный номер — не длиннее 50 символов";
        if (!Enum.IsDefined(f.Condition)) return "Выберите состояние из списка";
        return null;
    }
}
