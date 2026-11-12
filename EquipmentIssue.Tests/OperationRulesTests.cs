using EquipmentIssue.Models;
using EquipmentIssue.Services;

namespace EquipmentIssue.Tests;

/// <summary>Правила предметной области без базы данных.</summary>
public class OperationRulesTests
{
    private static readonly DateTime D = TestEnv.Today;

    private static Equipment Eq(EquipmentCondition c = EquipmentCondition.Good, bool writtenOff = false) =>
        new() { Id = 1, InventoryNumber = "OB-0001", Name = "Ноутбук Lenovo ThinkPad E15", Condition = c, IsWrittenOff = writtenOff };

    private static Employee Emp(bool active = true) =>
        new() { Id = 1, FullName = "Иванов Сергей Петрович", PersonnelNumber = "T-1001", Department = "Бухгалтерия", IsActive = active };

    [Fact]
    public void Дата_НеУказана_Ошибка() =>
        Assert.Equal("Укажите дату операции", OperationRules.ValidateDate(null, D, null));

    [Fact]
    public void Дата_Сегодня_Допустимо() =>
        Assert.Null(OperationRules.ValidateDate(D, D, null));

    [Fact]
    public void Дата_Завтра_Ошибка() =>
        Assert.Equal("Дата операции не может быть в будущем", OperationRules.ValidateDate(D.AddDays(1), D, null));

    [Fact]
    public void Дата_РаньшеПоследнейОперации_Ошибка() =>
        Assert.StartsWith("Дата операции не может быть раньше последней операции", OperationRules.ValidateDate(D.AddDays(-5), D, D.AddDays(-3)));

    [Fact]
    public void Дата_ВДеньПоследнейОперации_Допустимо() =>
        Assert.Null(OperationRules.ValidateDate(D.AddDays(-3), D, D.AddDays(-3)));

    [Fact]
    public void Выдача_ИсправноеНаСкладеРаботающему_Допустимо() =>
        Assert.Null(OperationRules.CheckIssue(Eq(), Emp(), null));

    [Fact]
    public void Выдача_СИзносом_Допустимо() =>
        Assert.Null(OperationRules.CheckIssue(Eq(EquipmentCondition.Worn), Emp(), null));

    [Fact]
    public void Выдача_УжеВыдано_ОтказСУказаниемДержателя()
    {
        var issue = new EquipmentOperation { Date = D.AddDays(-4), Employee = new Employee { FullName = "Кузнецов Алексей Игоревич" } };
        var error = OperationRules.CheckIssue(Eq(), Emp(), issue);
        Assert.NotNull(error);
        Assert.Contains("уже выдано", error);
        Assert.Contains("Кузнецов А. И.", error);
        Assert.Contains("08.11.2026", error);
    }

    [Fact]
    public void Выдача_ТребуетРемонта_Отказ() =>
        Assert.Contains("требует ремонта", OperationRules.CheckIssue(Eq(EquipmentCondition.NeedsRepair), Emp(), null));

    [Fact]
    public void Выдача_Списано_Отказ() =>
        Assert.Contains("списано", OperationRules.CheckIssue(Eq(writtenOff: true), Emp(), null));

    [Fact]
    public void Выдача_УволенномуСотруднику_Отказ() =>
        Assert.Equal("Сотрудник Иванов С. П. уволен — выдача невозможна", OperationRules.CheckIssue(Eq(), Emp(active: false), null));

    [Fact]
    public void Возврат_НеВыдано_Отказ() =>
        Assert.Contains("не числится выданным", OperationRules.CheckReturn(Eq(), null, EquipmentCondition.Good, null));

    [Fact]
    public void Возврат_БезСостояния_Ошибка() =>
        Assert.Equal("Укажите состояние оборудования при возврате",
            OperationRules.CheckReturn(Eq(), new EquipmentOperation(), null, null));

    [Fact]
    public void Возврат_ТребуетРемонтаБезКомментария_Ошибка() =>
        Assert.StartsWith("Опишите неисправность",
            OperationRules.CheckReturn(Eq(), new EquipmentOperation(), EquipmentCondition.NeedsRepair, "  "));

    [Fact]
    public void Возврат_ТребуетРемонтаСКомментарием_Допустимо() =>
        Assert.Null(OperationRules.CheckReturn(Eq(), new EquipmentOperation(), EquipmentCondition.NeedsRepair, "Разбит экран"));

    [Fact]
    public void Комментарий_200Символов_Допустимо() =>
        Assert.Null(OperationRules.CheckComment(new string('а', 200)));

    [Fact]
    public void Комментарий_201Символ_Ошибка() =>
        Assert.NotNull(OperationRules.CheckComment(new string('а', 201)));

    [Theory]
    [InlineData("Иванов Сергей Петрович", "Иванов С. П.")]
    [InlineData("Иванов Сергей", "Иванов С.")]
    [InlineData("Иванов", "Иванов")]
    public void КороткоеФИО(string full, string expected) =>
        Assert.Equal(expected, NameFormat.Short(full));
}
