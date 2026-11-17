using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueDesk.Features.Employees;

public class EmployeeConfiguration : IEntityTypeConfiguration<Domain.Employee>
{
    public void Configure(EntityTypeBuilder<Domain.Employee> e)
    {
        e.ToTable("Employees");
        e.Property(x => x.PersonnelNumber).HasMaxLength(6).IsFixedLength().IsRequired();
        e.HasIndex(x => x.PersonnelNumber).IsUnique();
        e.Property(x => x.LastName).HasMaxLength(40).IsRequired();
        e.Property(x => x.FirstName).HasMaxLength(40).IsRequired();
        e.Property(x => x.MiddleName).HasMaxLength(40);
        e.Property(x => x.Position).HasMaxLength(80).IsRequired();
        e.Property(x => x.Department).HasMaxLength(80).IsRequired();
        e.Property(x => x.Phone).HasMaxLength(20);
        e.Ignore(x => x.IsDismissed);
        e.Ignore(x => x.ShortName);
        e.Ignore(x => x.FullName);
    }
}
