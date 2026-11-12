using EquipmentIssue.Data;
using EquipmentIssue.Models;
using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentIssue.Tests;

/// <summary>
/// «Остановленные» часы: сегодняшняя дата не зависит от момента запуска тестов.
/// </summary>
public class FixedClock : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedClock(DateTime now) => _now = new DateTimeOffset(now, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>Тестовое окружение: пустая база SQLite в памяти и часы на 12.11.2026 10:00.</summary>
public sealed class TestEnv : IDisposable
{
    public static readonly DateTime Today = new(2026, 11, 12);
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public FixedClock Clock { get; } = new(Today.AddHours(10));

    public TestEnv()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = Db();
        db.Database.EnsureCreated();
    }

    /// <summary>Новый контекст к той же базе (как новый HTTP-запрос).</summary>
    public AppDbContext Db() => new(_options);

    public Equipment AddEquipment(string inv = "OB-0001", string name = "Ноутбук Lenovo ThinkPad E15",
        EquipmentCondition condition = EquipmentCondition.Good, bool writtenOff = false, string category = "Ноутбуки")
    {
        using var db = Db();
        var e = new Equipment { InventoryNumber = inv, Name = name, Category = category, Condition = condition, IsWrittenOff = writtenOff };
        db.Equipment.Add(e);
        db.SaveChanges();
        return e;
    }

    public Employee AddEmployee(string name = "Иванов Сергей Петрович", string tab = "T-1001",
        string department = "Бухгалтерия", bool active = true)
    {
        using var db = Db();
        var e = new Employee { FullName = name, PersonnelNumber = tab, Department = department, IsActive = active };
        db.Employees.Add(e);
        db.SaveChanges();
        return e;
    }

    /// <summary>Выполнить действие сервиса операций в отдельном контексте.</summary>
    public T Ops<T>(Func<OperationService, T> action)
    {
        using var db = Db();
        return action(new OperationService(db, Clock));
    }

    public OperationResult Issue(int equipmentId, int employeeId, int daysAgo = 0, string? comment = null) =>
        Ops(s => s.Issue(new IssueForm { EquipmentId = equipmentId, EmployeeId = employeeId, Date = Today.AddDays(-daysAgo), Comment = comment }));

    public OperationResult Return(int equipmentId, EquipmentCondition? condition = EquipmentCondition.Good, int daysAgo = 0, string? comment = null) =>
        Ops(s => s.Return(new ReturnForm { EquipmentId = equipmentId, Condition = condition, Date = Today.AddDays(-daysAgo), Comment = comment }));

    public int OperationCount()
    {
        using var db = Db();
        return db.Operations.Count();
    }

    public void Dispose() => _connection.Dispose();
}
