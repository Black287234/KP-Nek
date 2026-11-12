using EquipmentIssue.Models;
using EquipmentIssue.Services;

namespace EquipmentIssue.Tests;

/// <summary>Вычисление «у кого сейчас оборудование» по истории операций (без базы данных).</summary>
public class OnHandCalculatorTests
{
    private static readonly DateTime D = TestEnv.Today;

    private static EquipmentOperation Op(int id, int equipmentId, OperationType type, int daysAgo, int? employeeId = null) =>
        new() { Id = id, EquipmentId = equipmentId, Type = type, Date = D.AddDays(-daysAgo), EmployeeId = employeeId };

    [Fact]
    public void Держатель_НетОпераций_НаСкладе()
    {
        Assert.Null(OnHandCalculator.CurrentHolderId(new List<EquipmentOperation>()));
    }

    [Fact]
    public void Держатель_ПослеВыдачи_Сотрудник()
    {
        var ops = new[] { Op(1, 1, OperationType.Issue, 5, employeeId: 7) };
        Assert.Equal(7, OnHandCalculator.CurrentHolderId(ops));
    }

    [Fact]
    public void Держатель_ВыдачаИВозврат_НаСкладе()
    {
        var ops = new[] { Op(1, 1, OperationType.Issue, 5, 7), Op(2, 1, OperationType.Return, 2, 7) };
        Assert.Null(OnHandCalculator.CurrentHolderId(ops));
    }

    [Fact]
    public void Держатель_ПовторнаяВыдача_ВторойСотрудник()
    {
        var ops = new[] { Op(1, 1, OperationType.Issue, 9, 7), Op(2, 1, OperationType.Return, 5, 7), Op(3, 1, OperationType.Issue, 2, 8) };
        Assert.Equal(8, OnHandCalculator.CurrentHolderId(ops));
    }

    [Fact]
    public void Держатель_ПорядокПоДатеАНеПоСписку()
    {
        // Операции переданы не по порядку: возврат (позже) стоит раньше выдачи
        var ops = new[] { Op(2, 1, OperationType.Return, 1, 7), Op(1, 1, OperationType.Issue, 4, 7) };
        Assert.Null(OnHandCalculator.CurrentHolderId(ops));
    }

    [Fact]
    public void Держатель_ВозвратИВыдачаВОдинДень_ПоНомеруОперации()
    {
        var ops = new[] { Op(1, 1, OperationType.Issue, 3, 7), Op(2, 1, OperationType.Return, 0, 7), Op(3, 1, OperationType.Issue, 0, 8) };
        Assert.Equal(8, OnHandCalculator.CurrentHolderId(ops));
    }

    [Fact]
    public void Держатель_СменаСостоянияНеВлияет()
    {
        var ops = new[] { Op(1, 1, OperationType.ConditionChange, 3), Op(2, 1, OperationType.Issue, 2, 7) };
        Assert.Equal(7, OnHandCalculator.CurrentHolderId(ops));
    }

    [Fact]
    public void ТекущиеВыдачи_НесколькоЕдиниц_ТолькоВыданные()
    {
        var ops = new[]
        {
            Op(1, 1, OperationType.Issue, 10, 7),
            Op(2, 2, OperationType.Issue, 9, 8),
            Op(3, 1, OperationType.Return, 5, 7),
            Op(4, 3, OperationType.Issue, 1, 7)
        };
        var current = OnHandCalculator.CurrentIssues(ops);
        Assert.Equal(new[] { 2, 3 }, current.Keys.OrderBy(k => k));
        Assert.Equal(8, current[2].EmployeeId);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(30, 30)]
    [InlineData(-2, 0)]   // дата в будущем (ошибка данных) не даёт отрицательного числа
    public void ДнейНаРуках(int daysAgo, int expected)
    {
        Assert.Equal(expected, OnHandCalculator.DaysOnHand(D.AddDays(-daysAgo), D));
    }
}
