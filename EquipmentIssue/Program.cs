using EquipmentIssue.Data;
using EquipmentIssue.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Подключаем MVC: контроллеры + представления (страницы Razor)
builder.Services.AddControllersWithViews();

// База данных SQLite, строка подключения — в appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Сервисы с предметной логикой. Scoped — один объект на один HTTP-запрос
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<EmployeeService>();

var app = builder.Build();

// При запуске применяем миграции (создаём или обновляем базу) и добавляем учебные данные
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
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
