using EquipmentIssue.Models;
using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;

namespace EquipmentIssue.Tests;

/// <summary>Списание (вместо удаления) и изменение состояния на складе.</summary>
public class WriteOffAndConditionTests
{
    private static WriteOffForm WriteOff(string reason) => new() { Reason = reason, Date = TestEnv.Today };

    [Fact]
    public void Списание_НаСкладе_УспехИВыдачаЗапрещена()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();

        var result = env.Ops(s => s.WriteOff(eq.Id, WriteOff("Разбита матрица, ремонт дороже нового")));

        Assert.True(result.Success, result.Error);
        using (var db = env.Db())
        {
            var item = db.Equipment.Single();
            Assert.True(item.IsWrittenOff);
            Assert.Equal("Разбита матрица, ремонт дороже нового", item.WriteOffReason);
            Assert.Equal(OperationType.WriteOff, db.Operations.Single().Type);
        }
        Assert.Equal(ErrorKind.Conflict, env.Issue(eq.Id, emp.Id).Kind);
    }

    [Fact]
    public void Списание_ВыданногоОборудования_Отказ()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 1);

        var result = env.Ops(s => s.WriteOff(eq.Id, WriteOff("Устарело")));

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("Сначала оформите возврат", result.Error);
    }

    [Fact]
    public void Списание_КороткаяПричина_Ошибка()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();

        Assert.Equal(ErrorKind.Invalid, env.Ops(s => s.WriteOff(eq.Id, WriteOff("тест"))).Kind);
    }

    [Fact]
    public void Списание_Повторное_Отказ()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment(writtenOff: true);

        Assert.Equal("Оборудование уже списано", env.Ops(s => s.WriteOff(eq.Id, WriteOff("Повторное списание"))).Error);
    }

    [Fact]
    public void Состояние_ПослеРемонта_ВыдачаСноваРазрешена()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment(condition: EquipmentCondition.NeedsRepair);
        var emp = env.AddEmployee();

        var change = env.Ops(s => s.ChangeCondition(eq.Id,
            new ConditionForm { Condition = EquipmentCondition.Good, Comment = "Заменён аккумулятор", Date = TestEnv.Today }));
        var issue = env.Issue(eq.Id, emp.Id);

        Assert.True(change.Success, change.Error);
        Assert.True(issue.Success, issue.Error);
    }

    [Fact]
    public void Состояние_УВыданногоОборудования_Отказ()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 1);

        var result = env.Ops(s => s.ChangeCondition(eq.Id,
            new ConditionForm { Condition = EquipmentCondition.Worn, Comment = "Износ", Date = TestEnv.Today }));

        Assert.Equal(ErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public void Состояние_БезКомментария_Ошибка()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment(condition: EquipmentCondition.NeedsRepair);

        var result = env.Ops(s => s.ChangeCondition(eq.Id,
            new ConditionForm { Condition = EquipmentCondition.Good, Date = TestEnv.Today }));

        Assert.Equal(ErrorKind.Invalid, result.Kind);
    }
}
