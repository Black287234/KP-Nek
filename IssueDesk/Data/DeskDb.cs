using Microsoft.EntityFrameworkCore;

namespace IssueDesk.Data;

/// <summary>
/// Контекст базы данных. Настройки таблиц лежат рядом с функциями в папках Features
/// (классы IEntityTypeConfiguration) и подключаются автоматически.
/// </summary>
public class DeskDb(DbContextOptions<DeskDb> options) : DbContext(options)
{
    public DbSet<Domain.Equipment> Equipment => Set<Domain.Equipment>();
    public DbSet<Domain.Employee> Employees => Set<Domain.Employee>();

    protected override void OnModelCreating(ModelBuilder model) =>
        model.ApplyConfigurationsFromAssembly(typeof(DeskDb).Assembly);
}
