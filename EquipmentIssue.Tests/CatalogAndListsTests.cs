using EquipmentIssue.Models;
using EquipmentIssue.Services;
using EquipmentIssue.ViewModels;

namespace EquipmentIssue.Tests;

/// <summary>Справочники, список «у сотрудников», история, сводка и сохранение данных.</summary>
public class CatalogAndListsTests
{
    [Fact]
    public void Оборудование_ПовторИнвентарногоНомераБезУчётаРегистра_Отказ()
    {
        using var env = new TestEnv();
        env.AddEquipment("OB-0001");
        using var db = env.Db();
        var service = new EquipmentService(db);

        var result = service.Create(new EquipmentForm { InventoryNumber = " ob-0001 ", Name = "Другой ноутбук", Category = "Ноутбуки" });

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Equal("Оборудование с инвентарным номером OB-0001 уже есть в справочнике", result.Error);
    }

    [Fact]
    public void Оборудование_ПустоеНаименование_Ошибка()
    {
        using var env = new TestEnv();
        using var db = env.Db();

        var result = new EquipmentService(db).Create(new EquipmentForm { InventoryNumber = "OB-0009", Name = "  ", Category = "Ноутбуки" });

        Assert.Equal("Укажите наименование", result.Error);
    }

    [Fact]
    public void Оборудование_ПоискПоРусскиБезУчётаРегистра()
    {
        using var env = new TestEnv();
        env.AddEquipment("OB-0001", "Ноутбук Lenovo");
        env.AddEquipment("OB-0101", "Монитор Samsung", category: "Мониторы");
        using var db = env.Db();

        var found = new EquipmentService(db).Search(new EquipmentFilter { Search = "НОУТБУК" });

        Assert.Single(found);
        Assert.Equal("OB-0001", found[0].InventoryNumber);
    }

    [Fact]
    public void Оборудование_ФильтрМестонахождения()
    {
        using var env = new TestEnv();
        var a = env.AddEquipment("OB-0001");
        env.AddEquipment("OB-0002", "Ноутбук HP");
        var emp = env.AddEmployee();
        env.Issue(a.Id, emp.Id);
        using var db = env.Db();
        var issued = new OperationService(db, env.Clock).CurrentIssues();
        var service = new EquipmentService(db);

        Assert.Equal("OB-0001", service.Search(new EquipmentFilter { Location = "issued" }, issued).Single().InventoryNumber);
        Assert.Equal("OB-0002", service.Search(new EquipmentFilter { Location = "stock" }, issued).Single().InventoryNumber);
    }

    [Fact]
    public void Сотрудник_УвольнениеСОборудованиемНаРуках_Отказ()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 1);
        using var db = env.Db();

        var result = new EmployeeService(db).SetActive(emp.Id, false);

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("на руках оборудование (1 ед.)", result.Error);
    }

    [Fact]
    public void Сотрудник_УвольнениеПослеВозврата_Допустимо()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 2);
        env.Return(eq.Id);
        using var db = env.Db();

        Assert.True(new EmployeeService(db).SetActive(emp.Id, false).Success);
    }

    [Fact]
    public void Сотрудник_ПовторТабельногоНомера_Отказ()
    {
        using var env = new TestEnv();
        env.AddEmployee(tab: "T-1001");
        using var db = env.Db();

        var result = new EmployeeService(db).Create(new EmployeeForm { FullName = "Петров Пётр", PersonnelNumber = "t-1001", Department = "IT-отдел" });

        Assert.Equal(ErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public void НаРуках_ФильтрПоПодразделениюИДни()
    {
        using var env = new TestEnv();
        var a = env.AddEquipment("OB-0001");
        var b = env.AddEquipment("OB-0002", "Ноутбук HP");
        var buh = env.AddEmployee();
        var it = env.AddEmployee("Кузнецов Алексей Игоревич", "T-1003", "IT-отдел");
        env.Issue(a.Id, buh.Id, daysAgo: 10);
        env.Issue(b.Id, it.Id, daysAgo: 3);

        var rows = env.Ops(s => s.GetOnHand(new OnHandFilter { Department = "IT-отдел" }));

        var row = Assert.Single(rows);
        Assert.Equal("OB-0002", row.Equipment.InventoryNumber);
        Assert.Equal(3, row.DaysOnHand);
    }

    [Fact]
    public void История_ФильтрПоВидуИПериоду()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 30);
        env.Return(eq.Id, daysAgo: 20);
        env.Issue(eq.Id, emp.Id, daysAgo: 5);

        var issues = env.Ops(s => s.GetHistory(new HistoryFilter { Type = OperationType.Issue }));
        var lastWeek = env.Ops(s => s.GetHistory(new HistoryFilter { From = TestEnv.Today.AddDays(-7), To = TestEnv.Today }));
        var all = env.Ops(s => s.GetHistory(new HistoryFilter()));

        Assert.Equal(2, issues.Count);
        Assert.Single(lastWeek);
        Assert.Equal(3, all.Count);
        Assert.Equal(OperationType.Issue, all[0].Type);   // новые сверху
        Assert.Equal(TestEnv.Today.AddDays(-5), all[0].Date);
    }

    [Fact]
    public void Сводка_СчитаетВыданноеИНаСкладе()
    {
        using var env = new TestEnv();
        var a = env.AddEquipment("OB-0001");
        env.AddEquipment("OB-0002", "Ноутбук HP", EquipmentCondition.NeedsRepair);
        env.AddEquipment("OB-0003", "Проектор", writtenOff: true);
        var emp = env.AddEmployee();
        env.Issue(a.Id, emp.Id);

        var s = env.Ops(x => x.GetSummary());

        Assert.Equal(2, s.Total);
        Assert.Equal(1, s.Issued);
        Assert.Equal(1, s.InStock);
        Assert.Equal(1, s.NeedsRepair);
        Assert.Equal(1, s.WrittenOff);
        Assert.Equal(1, s.EmployeesWithEquipment);
    }

    [Fact]
    public void Сохранение_ДанныеЧитаютсяНовымКонтекстом()
    {
        using var env = new TestEnv();
        var eq = env.AddEquipment();
        var emp = env.AddEmployee();
        env.Issue(eq.Id, emp.Id, daysAgo: 1, comment: "Проверка сохранения");

        using var db = env.Db();   // новый контекст — как после перезапуска приложения
        var op = db.Operations.Single();
        Assert.Equal("Проверка сохранения", op.Comment);
        Assert.Equal(emp.Id, op.EmployeeId);
    }

    [Fact]
    public void УчебныеДанные_ЗагружаютсяИСогласованы()
    {
        using var env = new TestEnv();
        using (var db = env.Db())
            EquipmentIssue.Data.DbSeeder.Seed(db, TestEnv.Today);

        var s = env.Ops(x => x.GetSummary());
        var onHand = env.Ops(x => x.GetOnHand(new OnHandFilter()));

        Assert.Equal(11, s.Total);
        Assert.Equal(7, s.Issued);
        Assert.Equal(4, s.InStock);
        Assert.Equal(7, onHand.Count);
        Assert.Equal(18, env.OperationCount());
    }
}
