using IssueDesk.Components;
using IssueDesk.Data;
using IssueDesk.Features.Employees;
using IssueDesk.Features.Equipment;
using IssueDesk.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<DeskDb>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Desk")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<Notice>();
// Обработчики функций (вертикальные срезы): по одному классу на действие
builder.Services.AddScoped<ListEquipment>();
builder.Services.AddScoped<GetEquipment>();
builder.Services.AddScoped<SaveEquipment>();
builder.Services.AddScoped<WriteOffEquipment>();
builder.Services.AddScoped<ListEmployees>();
builder.Services.AddScoped<GetEmployee>();
builder.Services.AddScoped<SaveEmployee>();
builder.Services.AddScoped<DismissEmployee>();

var app = builder.Build();

// База данных создаётся и обновляется по миграциям; пустая база заполняется учебными данными
using (var scope = app.Services.CreateScope())
{
    using var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DeskDb>>().CreateDbContext();
    db.Database.Migrate();
    Seed.Run(db, DateOnly.FromDateTime(DateTime.Today));
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/error", createScopeForErrors: true);

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
