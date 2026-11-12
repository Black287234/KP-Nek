using EquipmentIssue.Models;

namespace EquipmentIssue.Data;

/// <summary>
/// Временное хранилище оборудования в памяти (только для этапа 1).
/// Список живёт, пока работает приложение, и сбрасывается при перезапуске.
/// На этапе 2 заменяется базой SQLite.
/// </summary>
public static class EquipmentStore
{
    public static readonly List<Equipment> Items = new()
    {
        new Equipment { Id = 1, InventoryNumber = "OB-0001", Name = "Ноутбук Lenovo ThinkPad E15", Category = "Ноутбуки", SerialNumber = "PF3KX81" },
        new Equipment { Id = 2, InventoryNumber = "OB-0002", Name = "Ноутбук HP ProBook 450 G9", Category = "Ноутбуки", SerialNumber = "5CD2341HQ", Condition = EquipmentCondition.Worn },
        new Equipment { Id = 3, InventoryNumber = "OB-0101", Name = "Монитор Samsung 24\" S24R350", Category = "Мониторы" },
        new Equipment { Id = 4, InventoryNumber = "OB-0102", Name = "Монитор AOC 27\" 27B2H", Category = "Мониторы" },
        new Equipment { Id = 5, InventoryNumber = "OB-0201", Name = "IP-телефон Yealink T31P", Category = "Телефоны" },
        new Equipment { Id = 6, InventoryNumber = "OB-0301", Name = "Гарнитура Jabra Evolve 20", Category = "Периферия", Condition = EquipmentCondition.NeedsRepair },
        new Equipment { Id = 7, InventoryNumber = "OB-0401", Name = "Шуруповёрт Makita DF333D", Category = "Инструмент" },
        new Equipment { Id = 8, InventoryNumber = "OB-0501", Name = "Проектор Epson EB-E01", Category = "Презентационное", IsWrittenOff = true }
    };
}
