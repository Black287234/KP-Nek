using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueDesk.Features.Issuing;

public class AssignmentConfiguration : IEntityTypeConfiguration<Domain.Assignment>
{
    public void Configure(EntityTypeBuilder<Domain.Assignment> e)
    {
        e.ToTable("Assignments");
        e.Property(x => x.IssuedCondition).HasConversion<string>().HasMaxLength(15);
        e.Property(x => x.ReturnedCondition).HasConversion<string>().HasMaxLength(15);
        e.Property(x => x.IssueNote).HasMaxLength(200);
        e.Property(x => x.ReturnNote).HasMaxLength(200);
        e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        e.Ignore(x => x.IsOpen);
        // Единица оборудования может находиться только у одного сотрудника:
        // частичный уникальный индекс по незакрытым записям защищает от повторной выдачи на уровне базы данных
        e.HasIndex(x => x.EquipmentId).IsUnique().HasFilter("\"ReturnedOn\" IS NULL").HasDatabaseName("UX_Assignments_OpenPerEquipment");
        e.HasIndex(x => new { x.EmployeeId, x.ReturnedOn });
        e.ToTable(t => t.HasCheckConstraint("CK_Assignments_Dates", "\"ReturnedOn\" IS NULL OR \"ReturnedOn\" >= \"IssuedOn\""));
    }
}
