using Community.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.API.Infrastructure.EntityConfigurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
       public void Configure(EntityTypeBuilder<Post> builder)
       {
              builder.ToTable("Posts");
              builder.HasKey(p => p.Id);

              builder.Property(p => p.AuthorId)
                     .IsRequired();

              builder.Property(p => p.Content)
                     .IsRequired()
                     .HasMaxLength(5000);

              builder.Property(p => p.MediaUrls)
                     .HasColumnType("jsonb");

              builder.Property(p => p.PostType)
                     .HasConversion<string>();

              builder.Property(p => p.IsHidden)
                     .HasDefaultValue(false);

              builder.HasIndex(p => p.AuthorId);
              builder.HasIndex(p => p.PostType);
              builder.HasIndex(p => p.IsHidden);
              builder.HasIndex(p => p.CreatedAt);
       }
}