using EquipmentIssue.Models;
using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;

namespace EquipmentIssue.Tests;

/// <summary>Выдача оборудования: главное правило и проверки данных (сервис + база SQLite в памяти).</summary>
public class IssueTests
{
    [Fact]
    public void Выдача_НаСкладе_УспехИОборудованиеУСотрудника()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();

        var result = env.Issue(eq.Id, emp.Id, comment: "Для удалённой работы");

        Assert.True(result.Success, result.Error);
        Assert.Equal("Оборудование «Ноутбук Lenovo ThinkPad E15» (OB-0001) выдано сотруднику Иванов С. П.", result.Message);
        var issue = env.Ops(s => s.CurrentIssue(eq.Id));
        Assert.NotNull(issue);
        Assert.Equal(emp.Id, issue.EmployeeId);
        Assert.Equal(TestEnv.Today, issue.Date);
    }

    [Fact]
    public void Выдача_УжеВыдано_Отказ409ИОперацияНеСохранена()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var first = env.AddEmployee();
        var second = env.AddEmployee("Смирнова Ольга Андреевна", "T-1002");
        env.Issue(eq.Id, first.Id, daysAgo: 3);

        var result = env.Issue(eq.Id, second.Id);

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("уже выдано: находится у Иванов С. П. с 09.11.2026", result.Error);
        Assert.Equal(1, env.OperationCount());
        Assert.Equal(first.Id, env.Ops(s => s.CurrentIssue(eq.Id))!.EmployeeId);
    }

    [Fact]
    public void Выдача_ТребуетРемонта_Отказ()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment(condition: EquipmentCondition.NeedsRepair);
        var emp = env.AddEmployee();

        var result = env.Issue(eq.Id, emp.Id);

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("требует ремонта", result.Error);
        Assert.Equal(0, env.OperationCount());
    }

    [Fact]
    public void Выдача_Списанного_Отказ()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment(writtenOff: true);
        var emp = env.AddEmployee();

        Assert.Equal(ErrorKind.Conflict, env.Issue(eq.Id, emp.Id).Kind);
    }

    [Fact]
    public void Выдача_УволенномуСотруднику_Отказ()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee(active: false);

        var result = env.Issue(eq.Id, emp.Id);

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("уволен", result.Error);
    }

    [Fact]
    public void Выдача_БезОборудования_Ошибка()
    {
        using var env = new TestEnv();
        var emp = env.AddEmployee();

        var result = env.Ops(s => s.Issue(new IssueForm { EmployeeId = emp.Id, Date = TestEnv.Today }));

        Assert.Equal(ErrorKind.Invalid, result.Kind);
        Assert.Equal("Выберите оборудование", result.Error);
    }

    [Fact]
    public void Выдача_БезСотрудника_Ошибка()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();

        var result = env.Ops(s => s.Issue(new IssueForm { EquipmentId = eq.Id, Date = TestEnv.Today }));

        Assert.Equal("Выберите сотрудника", result.Error);
    }

    [Fact]
    public void Выдача_НеизвестноеОборудование_404()
    {
        using var env = new TestEnv();
        var emp = env.AddEmployee();

        Assert.Equal(ErrorKind.NotFound, env.Issue(999, emp.Id).Kind);
    }

    [Fact]
    public void Выдача_ДатаВБудущем_Ошибка()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();

        var result = env.Issue(eq.Id, emp.Id, daysAgo: -1);

        Assert.Equal(ErrorKind.Invalid, result.Kind);
        Assert.Equal("Дата операции не может быть в будущем", result.Error);
    }

    [Fact]
    public void Выдача_ДатаРаньшеВозврата_Ошибка()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 10);
        env.Return(eq.Id, daysAgo: 5);

        var result = env.Issue(eq.Id, emp.Id, daysAgo: 7);

        Assert.Equal(ErrorKind.Invalid, result.Kind);
        Assert.StartsWith("Дата операции не может быть раньше последней операции", result.Error);
    }

    [Fact]
    public void Выдача_ВДеньВозврата_Допустимо()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var a = env.AddEmployee();
        var b = env.AddEmployee("Смирнова Ольга Андреевна", "T-1002");
        env.Issue(eq.Id, a.Id, daysAgo: 4);
        env.Return(eq.Id, daysAgo: 0);

        var result = env.Issue(eq.Id, b.Id, daysAgo: 0);

        Assert.True(result.Success, result.Error);
        Assert.Equal(b.Id, env.Ops(s => s.CurrentIssue(eq.Id))!.EmployeeId);
    }

    [Fact]
    public void Выдача_ДлинныйКомментарий_Ошибка()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();

        Assert.Equal(ErrorKind.Invalid, env.Issue(eq.Id, emp.Id, comment: new string('к', 201)).Kind);
    }
}
