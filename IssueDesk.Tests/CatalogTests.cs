using IssueDesk.Domain;
using IssueDesk.Features.Employees;
using IssueDesk.Features.Equipment;
using IssueDesk.Features.Issuing;
using IssueDesk.Shared;

namespace IssueDesk.Tests;

public sealed class CatalogTests : IDisposable
{
    private readonly Desk _desk = new();
    public void Dispose() => _desk.Dispose();

    private Result<int> SaveItem(string number, int? id = null) =>
        new SaveEquipment(_desk).Handle(id, new EquipmentForm { InventoryNumber = number, Name = "  Планшет   Samsung ", Kind = EquipmentKind.Tablet, SerialNumber = " r9jt " });

    [Fact]
    public void equipment_number_and_text_are_normalized()
    {
        var r = SaveItem(" ин-000401 ");
        var item = new GetEquipment(_desk).Handle(r.Value)!;
        Assert.Equal("ИН-000401", item.InventoryNumber);
        Assert.Equal("Планшет Samsung", item.Name);
        Assert.Equal("R9JT", item.SerialNumber);
    }

    [Fact]
    public void duplicate_inventory_number_is_rejected()
    {
        var r = SaveItem("ин-000101");
        Assert.Equal(Failure.Conflict, r.Failure);
        Assert.Equal("Оборудование с инвентарным номером ИН-000101 уже есть в перечне", r.Error);
    }

    [Fact]
    public void written_off_equipment_cannot_be_edited() =>
        Assert.Equal("Списанное оборудование не изменяется", SaveItem("ИН-000090", _desk.OldProjector).Error);

    [Theory]
    [InlineData(null)]
    [InlineData("сломан")]
    public void write_off_requires_reason(string? reason) =>
        Assert.Equal(Failure.Invalid, new WriteOffEquipment(_desk, _desk.Clock).Handle(_desk.Projector, reason).Failure);

    [Fact]
    public void issued_equipment_cannot_be_written_off()
    {
        new IssueEquipment(_desk, _desk.Clock).Handle(new IssueForm { EquipmentId = _desk.Projector, EmployeeId = _desk.Petrov, IssuedOn = _desk.Today });
        var r = new WriteOffEquipment(_desk, _desk.Clock).Handle(_desk.Projector, "Разбита линза объектива");
        Assert.Equal("Оборудование находится у сотрудника: сначала оформите возврат", r.Error);
    }

    [Fact]
    public void write_off_stores_date_and_reason()
    {
        Assert.True(new WriteOffEquipment(_desk, _desk.Clock).Handle(_desk.Projector, "Разбита линза объектива").Ok);
        var item = new GetEquipment(_desk).Handle(_desk.Projector)!;
        Assert.Equal(_desk.Today, item.WrittenOffOn);
        Assert.True(item.IsWrittenOff);
    }

    [Fact]
    public void employee_name_is_capitalized()
    {
        var r = new SaveEmployee(_desk).Handle(null, new EmployeeForm
        {
            PersonnelNumber = "000300", LastName = "петрова-водкина", FirstName = "мАРИЯ", MiddleName = "  ", Position = "Лаборант", Department = "Учебная часть"
        });
        var e = new GetEmployee(_desk).Handle(r.Value)!;
        Assert.Equal("Петрова-Водкина Мария", e.FullName);
        Assert.Equal("Петрова-Водкина М.", e.ShortName);
    }

    [Fact]
    public void duplicate_personnel_number_is_rejected()
    {
        var r = new SaveEmployee(_desk).Handle(null, new EmployeeForm
        {
            PersonnelNumber = "000101", LastName = "Новикова", FirstName = "Ирина", Position = "Методист", Department = "Учебная часть"
        });
        Assert.Equal("Сотрудник с табельным номером 000101 уже есть в справочнике", r.Error);
    }

    [Fact]
    public void employee_holding_equipment_cannot_be_dismissed()
    {
        new IssueEquipment(_desk, _desk.Clock).Handle(new IssueForm { EquipmentId = _desk.Laptop, EmployeeId = _desk.Ivanova, IssuedOn = _desk.Today });
        Assert.Equal("За сотрудником числится оборудование (1 ед.): сначала оформите возврат",
            new DismissEmployee(_desk, _desk.Clock).Handle(_desk.Ivanova).Error);
    }

    [Fact]
    public void dismissal_is_recorded_once()
    {
        var dismiss = new DismissEmployee(_desk, _desk.Clock);
        Assert.True(dismiss.Handle(_desk.Petrov).Ok);
        Assert.Equal(Failure.Conflict, dismiss.Handle(_desk.Petrov).Failure);
    }

    [Fact]
    public void dismissed_employees_are_hidden_by_default()
    {
        var list = new ListEmployees(_desk);
        Assert.Equal(2, list.Handle(new EmployeeFilter()).Count);
        Assert.Equal(3, list.Handle(new EmployeeFilter(WithDismissed: true)).Count);
        Assert.Single(list.Handle(new EmployeeFilter(Text: "ИВАНОВА")));
    }

    [Fact]
    public void equipment_search_covers_name_number_and_serial()
    {
        var list = new ListEquipment(_desk);
        Assert.Single(list.Handle(new EquipmentFilter(Text: "фото")));
        Assert.Single(list.Handle(new EquipmentFilter(Text: "000201")));
        Assert.Equal(4, list.Handle(new EquipmentFilter()).Count);
        Assert.Equal(5, list.Handle(new EquipmentFilter(WithWrittenOff: true)).Count);
    }

    [Theory]
    [InlineData("  иванов  ", "Иванов")]
    [InlineData("ЛЕБЕДЕВ-кумач", "Лебедев-Кумач")]
    [InlineData("   ", null)]
    public void capitalize_handles_case_hyphen_and_blank(string input, string? expected) =>
        Assert.Equal(expected, Text.Capitalize(input));
}
