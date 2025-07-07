using DT_HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_HR.Persistence.Configurations;

public class CompanyConfigurations: IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.Property(q => q.Name);
        builder.Property(q => q.DefaultWorkStartTime);
        builder.Property(q => q.DefaultWorkEndTime);
        builder.Property(q => q.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
        builder.Property(q => q.CreatedAt)
            .IsRequired();
        builder.Property(q => q.UpdatedAt);
        builder.Property(q => q.DeletedAt);
        builder.Property(q => q.IsDelete)
            .HasDefaultValue(false)
            .IsRequired();
    }
}