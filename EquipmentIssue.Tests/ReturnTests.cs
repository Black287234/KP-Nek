using EquipmentIssue.Models;
using EquipmentIssue.Services;

namespace EquipmentIssue.Tests;

/// <summary>Возврат оборудования и фиксация состояния.</summary>
public class ReturnTests
{
    [Fact]
    public void Возврат_Выданного_НаСкладеИСостояниеОбновлено()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 20);

        var result = env.Return(eq.Id, EquipmentCondition.Worn, comment: "Потёртости на крышке");

        Assert.True(result.Success, result.Error);
        Assert.Equal("Оборудование «Ноутбук Lenovo ThinkPad E15» возвращено от Иванов С. П., состояние: Есть износ", result.Message);
        Assert.Null(env.Ops(s => s.CurrentIssue(eq.Id)));
        using var db = env.Db();
        Assert.Equal(EquipmentCondition.Worn, db.Equipment.Single().Condition);
        var ret = db.Operations.Single(o => o.Type == OperationType.Return);
        Assert.Equal(emp.Id, ret.EmployeeId);   // сотрудник подставлен автоматически
    }

    [Fact]
    public void Возврат_НеВыданного_Отказ409()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();

        var result = env.Return(eq.Id);

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("не числится выданным", result.Error);
    }

    [Fact]
    public void Возврат_ТребуетРемонтаБезКомментария_Ошибка()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 2);

        var result = env.Return(eq.Id, EquipmentCondition.NeedsRepair);

        Assert.Equal(ErrorKind.Invalid, result.Kind);
        Assert.NotNull(env.Ops(s => s.CurrentIssue(eq.Id)));   // остаётся у сотрудника
    }

    [Fact]
    public void Возврат_ТребуетРемонта_ПовторнаяВыдачаЗаблокирована()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 2);

        var ret = env.Return(eq.Id, EquipmentCondition.NeedsRepair, comment: "Не включается");
        var again = env.Issue(eq.Id, emp.Id);

        Assert.True(ret.Success, ret.Error);
        Assert.EndsWith("Выдача заблокирована до ремонта", ret.Message);
        Assert.Equal(ErrorKind.Conflict, again.Kind);
    }

    [Fact]
    public void Возврат_БезСостояния_Ошибка()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 1);

        Assert.Equal("Укажите состояние оборудования при возврате", env.Return(eq.Id, condition: null).Error);
    }

    [Fact]
    public void Возврат_ДатаРаньшеВыдачи_Ошибка()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 3);

        var result = env.Return(eq.Id, daysAgo: 5);

        Assert.Equal(ErrorKind.Invalid, result.Kind);
        Assert.NotNull(env.Ops(s => s.CurrentIssue(eq.Id)));
    }
}
