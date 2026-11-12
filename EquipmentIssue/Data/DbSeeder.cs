using EquipmentIssue.Models;

namespace EquipmentIssue.Data;

/// <summary>Учебные данные: добавляются, если база пустая.</summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Equipment.Any()) return;

        db.Equipment.AddRange(
            new Equipment { InventoryNumber = "OB-0001", Name = "Ноутбук Lenovo ThinkPad E15", Category = "Ноутбуки", SerialNumber = "PF3KX81" },
            new Equipment { InventoryNumber = "OB-0002", Name = "Ноутбук HP ProBook 450 G9", Category = "Ноутбуки", SerialNumber = "5CD2341HQ", Condition = EquipmentCondition.Worn },
            new Equipment { InventoryNumber = "OB-0003", Name = "Ноутбук ASUS ExpertBook B1", Category = "Ноутбуки", SerialNumber = "N3NRKD01" },
            new Equipment { InventoryNumber = "OB-0101", Name = "Монитор Samsung 24\" S24R350", Category = "Мониторы" },
            new Equipment { InventoryNumber = "OB-0102", Name = "Монитор AOC 27\" 27B2H", Category = "Мониторы" },
            new Equipment { InventoryNumber = "OB-0201", Name = "IP-телефон Yealink T31P", Category = "Телефоны" },
            new Equipment { InventoryNumber = "OB-0202", Name = "Смартфон Samsung Galaxy A35", Category = "Телефоны", SerialNumber = "R58X20AB1" },
            new Equipment { InventoryNumber = "OB-0301", Name = "Гарнитура Jabra Evolve 20", Category = "Периферия", Condition = EquipmentCondition.NeedsRepair },
            new Equipment { InventoryNumber = "OB-0302", Name = "Веб-камера Logitech C920", Category = "Периферия" },
            new Equipment { InventoryNumber = "OB-0401", Name = "Шуруповёрт Makita DF333D", Category = "Инструмент" },
            new Equipment { InventoryNumber = "OB-0402", Name = "Мультиметр UNI-T UT33D", Category = "Инструмент" },
            new Equipment { InventoryNumber = "OB-0501", Name = "Проектор Epson EB-E01", Category = "Презентационное", IsWrittenOff = true, WriteOffReason = "Перегорела лампа, ремонт нецелесообразен" });

        db.Employees.AddRange(
            new Employee { FullName = "Иванов Сергей Петрович", PersonnelNumber = "T-1001", Department = "Бухгалтерия", Position = "Главный бухгалтер" },
            new Employee { FullName = "Смирнова Ольга Андреевна", PersonnelNumber = "T-1002", Department = "Отдел кадров", Position = "Специалист по кадрам" },
            new Employee { FullName = "Кузнецов Алексей Игоревич", PersonnelNumber = "T-1003", Department = "IT-отдел", Position = "Системный администратор" },
            new Employee { FullName = "Попова Мария Викторовна", PersonnelNumber = "T-1004", Department = "Учебная часть", Position = "Методист" },
            new Employee { FullName = "Васильев Дмитрий Олегович", PersonnelNumber = "T-1005", Department = "Хозяйственный отдел", Position = "Техник" },
            new Employee { FullName = "Новикова Анна Сергеевна", PersonnelNumber = "T-1006", Department = "Приёмная", Position = "Секретарь" },
            new Employee { FullName = "Морозов Илья Николаевич", PersonnelNumber = "T-1007", Department = "IT-отдел", Position = "Программист" },
            new Employee { FullName = "Фёдорова Елена Павловна", PersonnelNumber = "T-1008", Department = "Бухгалтерия", Position = "Бухгалтер", IsActive = false });

        db.SaveChanges();
    }
}
