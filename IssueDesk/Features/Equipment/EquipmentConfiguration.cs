using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueDesk.Features.Equipment;

/// <summary>Таблица Equipment: ограничения длины, уникальный инвентарный номер, перечисления текстом.</summary>
public class EquipmentConfiguration : IEntityTypeConfiguration<Domain.Equipment>
{
    public void Configure(EntityTypeBuilder<Domain.Equipment> e)
    {
        e.ToTable("Equipment");
        e.Property(x => x.InventoryNumber).HasMaxLength(9).IsRequired();
        e.HasIndex(x => x.InventoryNumber).IsUnique();
        e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        e.Property(x => x.SerialNumber).HasMaxLength(40);
        e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(15);
        e.Property(x => x.Condition).HasConversion<string>().HasMaxLength(15);
        e.Property(x => x.WriteOffReason).HasMaxLength(200);
        e.Ignore(x => x.IsWrittenOff);
    }
}
