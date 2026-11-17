using IssueDesk.Data;
using IssueDesk.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IssueDesk.Tests;

/// <summary>Clock frozen at Tuesday 17.11.2026 10:00 so date rules do not depend on the day the tests run.</summary>
public sealed class FrozenClock : TimeProvider
{
    public static readonly DateOnly Today = new(2026, 11, 17);
    public override DateTimeOffset GetUtcNow() => new(Today.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>
/// In-memory SQLite database created by the application's own migrations, plus a small catalogue.
/// Every test class gets a fresh instance (xUnit creates the class per test).
/// </summary>
public sealed class Desk : IDbContextFactory<DeskDb>, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<DeskDb> _options;
    public FrozenClock Clock { get; } = new();
    public DateOnly Today => FrozenClock.Today;

    public int Laptop, Projector, Camera, BrokenSpeaker, OldProjector;
    public int Ivanova, Petrov, Dismissed;

    public Desk()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<DeskDb>().UseSqlite(_connection).Options;
        using var db = CreateDbContext();
        db.Database.Migrate();

        var e = new[]
        {
            new Equipment { InventoryNumber = "ИН-000101", Name = "Ноутбук", Kind = EquipmentKind.Laptop },
            new Equipment { InventoryNumber = "ИН-000201", Name = "Проектор", Kind = EquipmentKind.Projector },
            new Equipment { InventoryNumber = "ИН-000301", Name = "Фотоаппарат", Kind = EquipmentKind.Camera, Condition = Condition.Worn },
            new Equipment { InventoryNumber = "ИН-000502", Name = "Колонка", Kind = EquipmentKind.Audio, Condition = Condition.Broken },
            new Equipment { InventoryNumber = "ИН-000090", Name = "Старый проектор", Kind = EquipmentKind.Projector, WrittenOffOn = Today.AddDays(-50), WriteOffReason = "Выработан ресурс" }
        };
        var p = new[]
        {
            new Employee { PersonnelNumber = "000101", LastName = "Иванова", FirstName = "Анна", MiddleName = "Павловна", Position = "Методист", Department = "Учебная часть" },
            new Employee { PersonnelNumber = "000102", LastName = "Петров", FirstName = "Олег", Position = "Преподаватель", Department = "Отделение ИТ" },
            new Employee { PersonnelNumber = "000103", LastName = "Сидоров", FirstName = "Ян", Position = "Лаборант", Department = "Отделение ИТ", DismissedOn = Today.AddDays(-3) }
        };
        db.Equipment.AddRange(e);
        db.Employees.AddRange(p);
        db.SaveChanges();
        (Laptop, Projector, Camera, BrokenSpeaker, OldProjector) = (e[0].Id, e[1].Id, e[2].Id, e[3].Id, e[4].Id);
        (Ivanova, Petrov, Dismissed) = (p[0].Id, p[1].Id, p[2].Id);
    }

    public DeskDb CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
