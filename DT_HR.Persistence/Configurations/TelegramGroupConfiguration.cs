using DT_HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_HR.Persistence.Configurations;

public class TelegramGroupConfiguration : IEntityTypeConfiguration<TelegramGroup>
{
    public void Configure(EntityTypeBuilder<TelegramGroup> builder)
    {
        builder.ToTable("telegram_groups");
        builder.Property(g => g.ChatId).IsRequired();
        builder.Property(g => g.Title).IsRequired();
        builder.Property(g => g.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt);
        builder.Property(g => g.DeletedAt);
        builder.Property(g => g.IsDelete).HasDefaultValue(false).IsRequired();

    }
}