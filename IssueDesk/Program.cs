using IssueDesk.Components;
using IssueDesk.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Этап 1: перечень оборудования хранится в памяти процесса
builder.Services.AddSingleton<IReadOnlyList<Equipment>>(new List<Equipment>
{
    new() { Id = 1, InventoryNumber = "ИН-000101", Name = "Ноутбук Lenovo ThinkPad E14", Kind = EquipmentKind.Laptop, SerialNumber = "PF3K1A2B" },
    new() { Id = 2, InventoryNumber = "ИН-000102", Name = "Ноутбук HP ProBook 450", Kind = EquipmentKind.Laptop, SerialNumber = "5CD2381XYZ" },
    new() { Id = 3, InventoryNumber = "ИН-000201", Name = "Проектор Epson EB-X49", Kind = EquipmentKind.Projector, SerialNumber = "X6YK123456" },
    new() { Id = 4, InventoryNumber = "ИН-000301", Name = "Фотоаппарат Canon EOS 250D", Kind = EquipmentKind.Camera, Condition = Condition.Worn },
    new() { Id = 5, InventoryNumber = "ИН-000401", Name = "Планшет Samsung Galaxy Tab A8", Kind = EquipmentKind.Tablet }
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/error", createScopeForErrors: true);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
