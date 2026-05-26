using Community.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.API.Infrastructure.EntityConfigurations;

public class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> builder)
    {
        builder.ToTable("PostComments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.AuthorId).IsRequired();
        builder.Property(c => c.Content).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.IsHidden).HasDefaultValue(false);
        builder.Property(c => c.LikeCount).HasDefaultValue(0);

        builder.HasIndex(c => c.PostId);
        builder.HasIndex(c => c.AuthorId);
        builder.HasIndex(c => c.CreatedAt);

        builder.HasOne(c => c.Post)
               .WithMany()
               .HasForeignKey(c => c.PostId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}