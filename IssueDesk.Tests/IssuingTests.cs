using IssueDesk.Domain;
using IssueDesk.Features.Issuing;
using IssueDesk.Shared;
using Microsoft.EntityFrameworkCore;

namespace IssueDesk.Tests;

public sealed class IssuingTests : IDisposable
{
    private readonly Desk _desk = new();
    private IssueEquipment Issue => new(_desk, _desk.Clock);
    private ReturnEquipment Return => new(_desk, _desk.Clock);

    public void Dispose() => _desk.Dispose();

    private Result<int> Give(int equipment, int employee, int daysAgo = 0, int? dueInDays = null) =>
        Issue.Handle(new IssueForm
        {
            EquipmentId = equipment, EmployeeId = employee, IssuedOn = _desk.Today.AddDays(-daysAgo),
            DueOn = dueInDays is { } d ? _desk.Today.AddDays(-daysAgo + d) : null
        });

    private Result<int> TakeBack(int equipment, int daysAgo = 0, Condition condition = Condition.Good, string? note = null) =>
        Return.Handle(new ReturnForm { EquipmentId = equipment, ReturnedOn = _desk.Today.AddDays(-daysAgo), Condition = condition, Note = note });

    [Fact]
    public void issue_records_assignment_with_condition_at_issue()
    {
        var r = Give(_desk.Camera, _desk.Ivanova, dueInDays: 7);

        Assert.True(r.Ok, r.Error);
        using var db = _desk.CreateDbContext();
        var a = db.Assignments.Single();
        Assert.Equal(Condition.Worn, a.IssuedCondition);
        Assert.Equal(_desk.Today.AddDays(7), a.DueOn);
        Assert.True(a.IsOpen);
    }

    [Fact]
    public void second_issue_of_same_equipment_is_rejected_with_current_holder()
    {
        Give(_desk.Laptop, _desk.Ivanova);
        var r = Give(_desk.Laptop, _desk.Petrov);

        Assert.Equal(Failure.Conflict, r.Failure);
        Assert.Equal("ИН-000101 уже выдано: находится у сотрудника Иванова А. П. с 17.11.2026", r.Error);
    }

    [Fact]
    public void database_index_forbids_two_open_assignments_for_one_item()
    {
        // Защита на уровне базы данных на случай одновременной выдачи, минуя проверку в обработчике
        Give(_desk.Laptop, _desk.Ivanova);
        using var db = _desk.CreateDbContext();
        db.Assignments.Add(new Assignment { EquipmentId = _desk.Laptop, EmployeeId = _desk.Petrov, IssuedOn = _desk.Today });
        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Theory]
    [InlineData(Condition.NeedsRepair)]
    [InlineData(Condition.Broken)]
    public void faulty_equipment_is_not_issued(Condition condition)
    {
        using (var db = _desk.CreateDbContext())
        {
            db.Equipment.Find(_desk.Projector)!.Condition = condition;
            db.SaveChanges();
        }
        var r = Give(_desk.Projector, _desk.Ivanova);
        Assert.Equal(Failure.Conflict, r.Failure);
        Assert.Contains("неисправное оборудование не выдаётся", r.Error);
    }

    [Fact]
    public void written_off_equipment_is_not_issued() =>
        Assert.Contains("списано", Give(_desk.OldProjector, _desk.Ivanova).Error);

    [Fact]
    public void dismissed_employee_gets_nothing() =>
        Assert.Equal("Сотрудник Сидоров Я. уволен: выдача невозможна", Give(_desk.Laptop, _desk.Dismissed).Error);

    [Theory]
    [InlineData(-1, "позже сегодняшней")]
    [InlineData(31, "не ранее чем за 30 дней")]
    public void issue_date_must_be_within_last_30_days(int daysAgo, string message)
    {
        var r = Give(_desk.Laptop, _desk.Ivanova, daysAgo);
        Assert.Equal(Failure.Invalid, r.Failure);
        Assert.Contains(message, r.Error);
    }

    [Fact]
    public void issue_30_days_ago_is_allowed() => Assert.True(Give(_desk.Laptop, _desk.Ivanova, daysAgo: 30).Ok);

    [Theory]
    [InlineData(-1, "раньше даты выдачи")]
    [InlineData(366, "не более 365 дней")]
    public void due_date_is_checked(int dueInDays, string message) =>
        Assert.Contains(message, Give(_desk.Laptop, _desk.Ivanova, dueInDays: dueInDays).Error);

    [Fact]
    public void backdated_issue_into_previous_holders_period_is_rejected()
    {
        // Регрессия Б13: фотоаппарат был у Ивановой с −15 по −12 день
        Give(_desk.Camera, _desk.Ivanova, daysAgo: 15);
        TakeBack(_desk.Camera, daysAgo: 12, condition: Condition.Worn);

        var r = Give(_desk.Camera, _desk.Petrov, daysAgo: 14);

        Assert.Equal(Failure.Conflict, r.Failure);
        Assert.Equal("ИН-000301 в этот период числилось за сотрудником Иванова А. П. (с 02.11.2026 по 05.11.2026): дата выдачи — не ранее 05.11.2026", r.Error);
    }

    [Fact]
    public void issue_on_the_day_of_previous_return_is_allowed()
    {
        Give(_desk.Camera, _desk.Ivanova, daysAgo: 15);
        TakeBack(_desk.Camera, daysAgo: 12, condition: Condition.Worn);
        Assert.True(Give(_desk.Camera, _desk.Petrov, daysAgo: 12).Ok);
    }

    [Fact]
    public void return_closes_assignment_and_updates_equipment_condition()
    {
        Give(_desk.Laptop, _desk.Ivanova, daysAgo: 5);
        var r = TakeBack(_desk.Laptop, daysAgo: 1, Condition.NeedsRepair, "Трещина на корпусе, не заряжается");

        Assert.True(r.Ok, r.Error);
        using var db = _desk.CreateDbContext();
        Assert.Equal(Condition.NeedsRepair, db.Equipment.Find(_desk.Laptop)!.Condition);
        var a = db.Assignments.Single();
        Assert.False(a.IsOpen);
        Assert.Equal("Трещина на корпусе, не заряжается", a.ReturnNote);
    }

    [Fact]
    public void return_of_equipment_in_stock_is_rejected() =>
        Assert.Equal("Оборудование не числится выданным", TakeBack(_desk.Laptop).Error);

    [Fact]
    public void return_before_issue_date_is_rejected()
    {
        Give(_desk.Laptop, _desk.Ivanova, daysAgo: 2);
        Assert.Contains("раньше даты выдачи (15.11.2026)", TakeBack(_desk.Laptop, daysAgo: 3).Error);
    }

    [Fact]
    public void return_in_future_is_rejected()
    {
        Give(_desk.Laptop, _desk.Ivanova);
        Assert.Contains("позже сегодняшней", TakeBack(_desk.Laptop, daysAgo: -1).Error);
    }

    [Theory]
    [InlineData(Condition.Broken, null)]
    [InlineData(Condition.NeedsRepair, "сломан")]
    public void faulty_return_requires_description(Condition condition, string? note)
    {
        Give(_desk.Laptop, _desk.Ivanova);
        Assert.Contains("Опишите неисправность", TakeBack(_desk.Laptop, condition: condition, note: note).Error);
    }

    [Fact]
    public void holdings_are_grouped_by_employee_and_flag_overdue()
    {
        Give(_desk.Laptop, _desk.Ivanova, daysAgo: 10, dueInDays: 5);   // срок истёк 5 дней назад
        Give(_desk.Projector, _desk.Ivanova, daysAgo: 2, dueInDays: 5);
        Give(_desk.Camera, _desk.Petrov);

        var groups = new ListHoldings(_desk, _desk.Clock).Handle();

        Assert.Equal(new[] { "Иванова", "Петров" }, groups.Select(g => g.Key.LastName));
        Assert.Equal(2, groups[0].Count());
        Assert.Single(groups[0], h => h.Overdue);
        Assert.Single(new ListHoldings(_desk, _desk.Clock).Handle(overdueOnly: true).SelectMany(g => g));
    }

    [Fact]
    public void history_lists_issue_and_return_newest_first()
    {
        Give(_desk.Laptop, _desk.Ivanova, daysAgo: 5);
        TakeBack(_desk.Laptop, daysAgo: 2);
        Give(_desk.Laptop, _desk.Petrov, daysAgo: 1);

        var events = new ListHistory(_desk).Handle(new HistoryFilter(EquipmentId: _desk.Laptop)).Value!;

        Assert.Equal(new[] { "выдача 16.11 Петров", "возврат 15.11 Иванова", "выдача 12.11 Иванова" },
            events.Select(e => $"{(e.IsIssue ? "выдача" : "возврат")} {e.Date:dd.MM} {e.Assignment.Employee!.LastName}"));
    }

    [Fact]
    public void history_with_reversed_period_is_rejected() =>
        Assert.Equal("Начало периода позже его окончания",
            new ListHistory(_desk).Handle(new HistoryFilter(_desk.Today, _desk.Today.AddDays(-1))).Error);

    [Fact]
    public void available_list_excludes_issued_faulty_and_written_off()
    {
        Give(_desk.Laptop, _desk.Ivanova);
        var available = new IssuingChoices(_desk).Available().Select(e => e.InventoryNumber);
        Assert.Equal(new[] { "ИН-000201", "ИН-000301" }, available);
    }
}
