using EquipmentIssue.Data;
using EquipmentIssue.Infrastructure;
using EquipmentIssue.Services;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Подключаем MVC: контроллеры + представления (страницы Razor)
builder.Services.AddControllersWithViews(ValidationMessages.Configure)   // русские сообщения привязки данных
    // Перечисления в JSON передаются строками: "Issue", "NeedsRepair"
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    // Неверный JSON в запросе к API → 400 и { error } по-русски
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ValidationMessages.ApiResponse);

// База данных SQLite, строка подключения — в appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Сервисы с предметной логикой. Scoped — один объект на один HTTP-запрос
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<OperationService>();

// Часы приложения. В тестах вместо системных подставляются «остановленные» часы
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// При запуске применяем миграции (создаём или обновляем базу) и добавляем учебные данные
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db, TimeProvider.System.GetLocalNow().DateTime);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Нужен, чтобы тестовый проект мог обратиться к классу Program
public partial class Program { }
