using IssueDesk.Domain;

namespace IssueDesk.Data;

/// <summary>Учебные данные для пустой базы.</summary>
public static class Seed
{
    public static void Run(DeskDb db, DateOnly today)
    {
        if (!db.Equipment.Any()) Catalogs(db, today);
        if (!db.Assignments.Any()) Assignments(db, today);
    }

    private static void Catalogs(DeskDb db, DateOnly today)
    {
        db.Employees.AddRange(
            new Employee { PersonnelNumber = "000112", LastName = "Крючков", FirstName = "Виталий", MiddleName = "Владимирович", Position = "Преподаватель", Department = "Отделение информационных технологий", Phone = "+7 (3462) 55-01-12" },
            new Employee { PersonnelNumber = "000127", LastName = "Лаптева", FirstName = "Ольга", MiddleName = "Николаевна", Position = "Преподаватель", Department = "Отделение информационных технологий" },
            new Employee { PersonnelNumber = "000203", LastName = "Мельникова", FirstName = "Татьяна", MiddleName = "Андреевна", Position = "Главный бухгалтер", Department = "Бухгалтерия", Phone = "+7 (3462) 55-02-03" },
            new Employee { PersonnelNumber = "000215", LastName = "Сафонов", FirstName = "Денис", MiddleName = "Игоревич", Position = "Системный администратор", Department = "Отдел информатизации" },
            new Employee { PersonnelNumber = "000231", LastName = "Гарипова", FirstName = "Римма", MiddleName = "Фаритовна", Position = "Методист", Department = "Учебная часть" },
            new Employee { PersonnelNumber = "000244", LastName = "Бойко", FirstName = "Артём", MiddleName = "Сергеевич", Position = "Педагог-организатор", Department = "Отдел воспитательной работы" },
            new Employee { PersonnelNumber = "000250", LastName = "Тихонова", FirstName = "Марина", MiddleName = "Петровна", Position = "Секретарь приёмной комиссии", Department = "Приёмная комиссия" },
            new Employee { PersonnelNumber = "000098", LastName = "Орлов", FirstName = "Глеб", MiddleName = "Викторович", Position = "Лаборант", Department = "Отделение информационных технологий", DismissedOn = today.AddDays(-40) });

        db.Equipment.AddRange(
            new Equipment { InventoryNumber = "ИН-000101", Name = "Ноутбук Lenovo ThinkPad E14", Kind = EquipmentKind.Laptop, SerialNumber = "PF3K1A2B" },
            new Equipment { InventoryNumber = "ИН-000102", Name = "Ноутбук HP ProBook 450 G9", Kind = EquipmentKind.Laptop, SerialNumber = "5CD2381XYZ" },
            new Equipment { InventoryNumber = "ИН-000103", Name = "Ноутбук ASUS ExpertBook B1", Kind = EquipmentKind.Laptop, SerialNumber = "N7NXCV01", Condition = Condition.Worn },
            new Equipment { InventoryNumber = "ИН-000104", Name = "Ноутбук Acer TravelMate P2", Kind = EquipmentKind.Laptop, SerialNumber = "NXVLLER00", Condition = Condition.NeedsRepair },
            new Equipment { InventoryNumber = "ИН-000201", Name = "Проектор Epson EB-X49", Kind = EquipmentKind.Projector, SerialNumber = "X6YK123456" },
            new Equipment { InventoryNumber = "ИН-000202", Name = "Проектор BenQ MS560", Kind = EquipmentKind.Projector, SerialNumber = "PDJ9A0042" },
            new Equipment { InventoryNumber = "ИН-000301", Name = "Фотоаппарат Canon EOS 250D", Kind = EquipmentKind.Camera, SerialNumber = "0832045519", Condition = Condition.Worn },
            new Equipment { InventoryNumber = "ИН-000302", Name = "Видеокамера Sony HDR-CX405", Kind = EquipmentKind.Camera, SerialNumber = "4221873" },
            new Equipment { InventoryNumber = "ИН-000401", Name = "Планшет Samsung Galaxy Tab A8", Kind = EquipmentKind.Tablet, SerialNumber = "R9JT30ABC" },
            new Equipment { InventoryNumber = "ИН-000402", Name = "Планшет Huawei MatePad SE", Kind = EquipmentKind.Tablet },
            new Equipment { InventoryNumber = "ИН-000501", Name = "Радиомикрофон Shure BLX24", Kind = EquipmentKind.Audio },
            new Equipment { InventoryNumber = "ИН-000502", Name = "Колонка портативная JBL Eon One", Kind = EquipmentKind.Audio, Condition = Condition.Broken },
            new Equipment { InventoryNumber = "ИН-000601", Name = "Мультиметр цифровой UNI-T UT33D", Kind = EquipmentKind.Measuring },
            new Equipment { InventoryNumber = "ИН-000090", Name = "Проектор NEC V260", Kind = EquipmentKind.Projector, WrittenOffOn = today.AddDays(-90), WriteOffReason = "Выработан ресурс лампы, ремонт нецелесообразен" });
        db.SaveChanges();
    }

    /// <summary>Выдачи за последние недели: часть возвращена, часть у сотрудников, одна просрочена.</summary>
    private static void Assignments(DeskDb db, DateOnly today)
    {
        var eq = db.Equipment.ToDictionary(e => e.InventoryNumber);
        var emp = db.Employees.ToDictionary(e => e.PersonnelNumber);
        var at = today.ToDateTime(new TimeOnly(9, 0));
        Assignment Give(string inv, string person, int daysAgo, int? dueIn = null, int? returnedAgo = null, Condition? back = null, string? note = null, Condition? was = null) => new()
        {
            Equipment = eq[inv], Employee = emp[person], IssuedOn = today.AddDays(-daysAgo), IssuedCondition = was ?? eq[inv].Condition,
            DueOn = dueIn is { } d ? today.AddDays(-daysAgo + d) : null,
            ReturnedOn = returnedAgo is { } r ? today.AddDays(-r) : null, ReturnedCondition = returnedAgo is null ? null : back ?? eq[inv].Condition,
            ReturnNote = note, RecordedAt = at.AddDays(-daysAgo)
        };
        db.Assignments.AddRange(
            Give("ИН-000101", "000112", 25),
            Give("ИН-000201", "000112", 12, dueIn: 30),
            Give("ИН-000102", "000203", 20, dueIn: 14),                       // срок возврата прошёл
            Give("ИН-000401", "000231", 9, dueIn: 21),
            Give("ИН-000301", "000244", 28, dueIn: 7, returnedAgo: 21),
            Give("ИН-000301", "000250", 15, dueIn: 3, returnedAgo: 12),
            Give("ИН-000302", "000244", 6),
            Give("ИН-000104", "000127", 27, returnedAgo: 18, back: Condition.NeedsRepair, note: "Не включается после падения, повреждён разъём питания", was: Condition.Good),
            Give("ИН-000501", "000244", 14, dueIn: 2, returnedAgo: 11),
            Give("ИН-000601", "000215", 4));
        db.SaveChanges();
    }
}
