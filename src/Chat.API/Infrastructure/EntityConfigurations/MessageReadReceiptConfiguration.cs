using Chat.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chat.API.Infrastructure.EntityConfigurations;

public class MessageReadReceiptConfiguration : IEntityTypeConfiguration<MessageReadReceipt>
{
    public void Configure(EntityTypeBuilder<MessageReadReceipt> builder)
    {
        builder.ToTable("MessageReadReceipts");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired();

        // 1 user chỉ đọc 1 message 1 lần
        builder.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();
    }
}