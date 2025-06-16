using DT_HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_HR.Persistence.Configurations;

public class AttendanceConfigurations : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Date)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(item => item.CreatedAt)
            .IsRequired();

        builder.Property(item => item.UpdatedAt);

        builder.Property(item => item.DeletedAt);

        builder.Property(item => item.IsDelete)
            .HasDefaultValue(false)
            .IsRequired();
    }
}