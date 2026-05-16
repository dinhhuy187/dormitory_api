using Community.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.API.Infrastructure.EntityConfigurations;

public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        builder.ToTable("PostLikes");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId).IsRequired();

        // 1 user chỉ like 1 post 1 lần
        builder.HasIndex(l => new { l.PostId, l.UserId }).IsUnique();

        builder.HasOne(l => l.Post)
               .WithMany()
               .HasForeignKey(l => l.PostId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}