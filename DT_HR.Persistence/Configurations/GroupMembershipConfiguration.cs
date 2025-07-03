using DT_HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_HR.Persistence.Configurations;

public class GroupMembershipConfiguration : IEntityTypeConfiguration<GroupMembership>
{
    public void Configure(EntityTypeBuilder<GroupMembership> builder)
    {
        builder.ToTable("group_membership");

        builder.HasKey(g => new { g.UserId, g.GroupId });

        builder.Property(g => g.GroupId)
            .IsRequired();
        builder.Property(g => g.UserId)
            .IsRequired();
        builder.Property(g => g.IsAdmin)
            .HasDefaultValue(false)
            .IsRequired();
        builder.Property(g => g.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
        builder.Property(g => g.CreatedAt)
            .IsRequired();
        
        builder.Property(g => g.DeletedAt);
        
        builder.Property(g => g.UpdatedAt);
        
        builder.Property(g => g.IsDelete)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(g => g.Group)
            .WithMany()
            .HasForeignKey(g => g.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}