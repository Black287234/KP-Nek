using EquipmentIssue.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentIssue.Data;

/// <summary>Контекст базы данных: таблицы оборудования, сотрудников и операций.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EquipmentOperation> Operations => Set<EquipmentOperation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Equipment>().HasIndex(e => e.InventoryNumber).IsUnique();
        b.Entity<Employee>().HasIndex(e => e.PersonnelNumber).IsUnique();

        // Оборудование 1 — N операции. Restrict: нельзя удалить оборудование с историей
        b.Entity<EquipmentOperation>()
            .HasOne(o => o.Equipment).WithMany(e => e.Operations)
            .HasForeignKey(o => o.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Сотрудник 1 — N операции (выдачи и возвраты)
        b.Entity<EquipmentOperation>()
            .HasOne(o => o.Employee).WithMany(e => e.Operations)
            .HasForeignKey(o => o.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Индекс для быстрого поиска последней операции по оборудованию
        b.Entity<EquipmentOperation>().HasIndex(o => new { o.EquipmentId, o.Date });
    }
}
