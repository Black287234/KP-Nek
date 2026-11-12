using EquipmentIssue.Services;

var builder = WebApplication.CreateBuilder(args);

// Подключаем MVC: контроллеры + представления (страницы Razor)
builder.Services.AddControllersWithViews();

// Сервис оборудования. Контроллеры получают его через конструктор (внедрение зависимостей)
builder.Services.AddScoped<EquipmentService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // В рабочем режиме при ошибке показываем понятную страницу, а не текст исключения
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();   // файлы из wwwroot (css)
app.UseRouting();

// Маршрут по умолчанию: /Контроллер/Действие/Id
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
