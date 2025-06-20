using DT_HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_HR.Persistence.Configurations;

public class AttendanceConfigurations : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("attendances");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();
        
        builder.Property(a => a.UserId)
            .IsRequired();
        builder.Property(a => a.Date)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(a => a.CheckInTime);
        builder.Property(a => a.CheckOutTime);
        builder.Property(a => a.CheckInLatitude);
        builder.Property(a => a.CheckInLongitude);
        
        builder.Property(a => a.AbsenceReason);
        
        
        builder.Property(a => a.EstimatedArrivalTime);
        builder.Property(a => a.IsWithInOfficeRadius);
        
        
        builder.Property(item => item.CreatedAt)
            .IsRequired();

        builder.Property(item => item.UpdatedAt);

        builder.Property(item => item.DeletedAt);

        builder.Property(item => item.IsDelete)
            .HasDefaultValue(false)
            .IsRequired();
    }
}