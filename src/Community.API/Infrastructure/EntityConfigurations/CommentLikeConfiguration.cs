using Community.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.API.Infrastructure.EntityConfigurations;

public class CommentLikeConfiguration : IEntityTypeConfiguration<CommentLike>
{
    public void Configure(EntityTypeBuilder<CommentLike> builder)
    {
        builder.ToTable("CommentLikes");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId).IsRequired();

        // 1 user chỉ like 1 comment 1 lần
        builder.HasIndex(l => new { l.CommentId, l.UserId }).IsUnique();

        builder.HasOne(l => l.Comment)
               .WithMany()
               .HasForeignKey(l => l.CommentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}