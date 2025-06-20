using DT_HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_HR.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.Property(item => item.Id)
            .IsRequired();

        builder.HasKey(item => item.Id);
        

        builder.Property(item => item.FirstName)
            .IsRequired();

        builder.Property(item => item.LastName)
            .IsRequired();
        
        builder.Property(item => item.TelegramUserId)
            .IsRequired();
        
        builder.Property(item => item.PhoneNumber)
            .IsRequired();
        
        builder.Property(u => u.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
        
        builder.HasMany<Attendance>()
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId);
        
        builder.Property(item => item.CreatedAt)
            .IsRequired();

        builder.Property(item => item.UpdatedAt);

        builder.Property(item => item.DeletedAt);

        builder.Property(item => item.IsDelete)
            .HasDefaultValue(false)
            .IsRequired();
    }
}