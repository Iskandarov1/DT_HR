using DT_HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_HR.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();
        builder.Property(e => e.Description)
            .IsRequired();
        builder.Property(e => e.EventTime)
            .IsRequired();
        builder.Property(e => e.CreatedAt)
            .IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.IsDelete)
            .HasDefaultValue(false)
            .IsRequired();
    }
}