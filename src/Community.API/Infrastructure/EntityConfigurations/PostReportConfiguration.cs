using Community.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Community.API.Infrastructure.EntityConfigurations;

public class PostReportConfiguration : IEntityTypeConfiguration<PostReport>
{
    public void Configure(EntityTypeBuilder<PostReport> builder)
    {
        builder.ToTable("PostReports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReporterId).IsRequired();
        builder.Property(r => r.Reason).HasConversion<string>();
        builder.Property(r => r.Status).HasConversion<string>();
        builder.Property(r => r.Note).HasMaxLength(500);

        // 1 user chỉ report 1 post 1 lần
        builder.HasIndex(r => new { r.PostId, r.ReporterId }).IsUnique();
        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.Post)
               .WithMany()
               .HasForeignKey(r => r.PostId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}