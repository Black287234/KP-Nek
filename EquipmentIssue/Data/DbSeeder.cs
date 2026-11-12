using EquipmentIssue.Models;

namespace EquipmentIssue.Data;

/// <summary>
/// Учебные данные: добавляются, если база пустая.
/// Даты операций отсчитываются от дня первого запуска, чтобы история выглядела свежей.
/// </summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db, DateTime today)
    {
        if (db.Equipment.Any()) return;
        today = today.Date;

        var eq = new Dictionary<string, Equipment>
        {
            ["OB-0001"] = new() { InventoryNumber = "OB-0001", Name = "Ноутбук Lenovo ThinkPad E15", Category = "Ноутбуки", SerialNumber = "PF3KX81" },
            ["OB-0002"] = new() { InventoryNumber = "OB-0002", Name = "Ноутбук HP ProBook 450 G9", Category = "Ноутбуки", SerialNumber = "5CD2341HQ", Condition = EquipmentCondition.Worn },
            ["OB-0003"] = new() { InventoryNumber = "OB-0003", Name = "Ноутбук ASUS ExpertBook B1", Category = "Ноутбуки", SerialNumber = "N3NRKD01" },
            ["OB-0101"] = new() { InventoryNumber = "OB-0101", Name = "Монитор Samsung 24\" S24R350", Category = "Мониторы" },
            ["OB-0102"] = new() { InventoryNumber = "OB-0102", Name = "Монитор AOC 27\" 27B2H", Category = "Мониторы" },
            ["OB-0201"] = new() { InventoryNumber = "OB-0201", Name = "IP-телефон Yealink T31P", Category = "Телефоны" },
            ["OB-0202"] = new() { InventoryNumber = "OB-0202", Name = "Смартфон Samsung Galaxy A35", Category = "Телефоны", SerialNumber = "R58X20AB1" },
            ["OB-0301"] = new() { InventoryNumber = "OB-0301", Name = "Гарнитура Jabra Evolve 20", Category = "Периферия", Condition = EquipmentCondition.NeedsRepair },
            ["OB-0302"] = new() { InventoryNumber = "OB-0302", Name = "Веб-камера Logitech C920", Category = "Периферия" },
            ["OB-0401"] = new() { InventoryNumber = "OB-0401", Name = "Шуруповёрт Makita DF333D", Category = "Инструмент" },
            ["OB-0402"] = new() { InventoryNumber = "OB-0402", Name = "Мультиметр UNI-T UT33D", Category = "Инструмент" },
            ["OB-0501"] = new() { InventoryNumber = "OB-0501", Name = "Проектор Epson EB-E01", Category = "Презентационное", IsWrittenOff = true, WriteOffReason = "Перегорела лампа, ремонт нецелесообразен" }
        };
        db.Equipment.AddRange(eq.Values);

        var emp = new Dictionary<string, Employee>
        {
            ["T-1001"] = new() { FullName = "Иванов Сергей Петрович", PersonnelNumber = "T-1001", Department = "Бухгалтерия", Position = "Главный бухгалтер" },
            ["T-1002"] = new() { FullName = "Смирнова Ольга Андреевна", PersonnelNumber = "T-1002", Department = "Отдел кадров", Position = "Специалист по кадрам" },
            ["T-1003"] = new() { FullName = "Кузнецов Алексей Игоревич", PersonnelNumber = "T-1003", Department = "IT-отдел", Position = "Системный администратор" },
            ["T-1004"] = new() { FullName = "Попова Мария Викторовна", PersonnelNumber = "T-1004", Department = "Учебная часть", Position = "Методист" },
            ["T-1005"] = new() { FullName = "Васильев Дмитрий Олегович", PersonnelNumber = "T-1005", Department = "Хозяйственный отдел", Position = "Техник" },
            ["T-1006"] = new() { FullName = "Новикова Анна Сергеевна", PersonnelNumber = "T-1006", Department = "Приёмная", Position = "Секретарь" },
            ["T-1007"] = new() { FullName = "Морозов Илья Николаевич", PersonnelNumber = "T-1007", Department = "IT-отдел", Position = "Программист" },
            ["T-1008"] = new() { FullName = "Фёдорова Елена Павловна", PersonnelNumber = "T-1008", Department = "Бухгалтерия", Position = "Бухгалтер", IsActive = false }
        };
        db.Employees.AddRange(emp.Values);

        // Операции: (оборудование, сотрудник, вид, сколько дней назад, состояние, комментарий)
        void Op(string inv, string? tab, OperationType type, int daysAgo, EquipmentCondition condition, string? comment = null) =>
            db.Operations.Add(new EquipmentOperation
            {
                Equipment = eq[inv],
                Employee = tab == null ? null : emp[tab],
                Type = type,
                Date = today.AddDays(-daysAgo),
                Condition = condition,
                Comment = comment,
                CreatedAt = today.AddDays(-daysAgo).AddHours(10)
            });

        var good = EquipmentCondition.Good;
        var worn = EquipmentCondition.Worn;

        Op("OB-0102", "T-1008", OperationType.Issue, 200, good, "Рабочее место бухгалтера");
        Op("OB-0102", "T-1008", OperationType.Return, 100, good, "Возврат при увольнении");
        Op("OB-0003", "T-1007", OperationType.Issue, 90, good);
        Op("OB-0501", null, OperationType.WriteOff, 80, good, "Перегорела лампа, ремонт нецелесообразен");
        Op("OB-0002", "T-1001", OperationType.Issue, 60, good);
        Op("OB-0101", "T-1001", OperationType.Issue, 60, good, "Второй монитор");
        Op("OB-0301", "T-1004", OperationType.Issue, 50, good);
        Op("OB-0402", "T-1005", OperationType.Issue, 45, good, "Проверка электропроводки в корпусе Б");
        Op("OB-0001", "T-1003", OperationType.Issue, 40, good);
        Op("OB-0402", "T-1005", OperationType.Return, 35, good);
        Op("OB-0003", "T-1007", OperationType.Return, 30, good);
        Op("OB-0302", "T-1007", OperationType.Issue, 25, good, "Для видеоконференций");
        Op("OB-0002", "T-1001", OperationType.Return, 20, worn, "Потёртости на корпусе");
        Op("OB-0202", "T-1003", OperationType.Issue, 15, good, "Дежурный телефон");
        Op("OB-0201", "T-1006", OperationType.Issue, 12, good);
        Op("OB-0002", "T-1002", OperationType.Issue, 10, worn);
        Op("OB-0401", "T-1005", OperationType.Issue, 7, good);
        Op("OB-0301", "T-1004", OperationType.Return, 5, EquipmentCondition.NeedsRepair, "Не работает левый динамик");

        db.SaveChanges();
    }
}
