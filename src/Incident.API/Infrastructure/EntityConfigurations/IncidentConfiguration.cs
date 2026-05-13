using Incident.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Incident.API.Infrastructure.EntityConfigurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Domain.Entities.Incident>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Incident> builder)
    {
        builder.ToTable("Incidents");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ReporterId).IsRequired();
        builder.Property(i => i.Description)
               .IsRequired()
               .HasMaxLength(1000);
        builder.Property(i => i.Status)
               .HasConversion<string>();

        // Lưu List<string> dạng JSON column trên PostgreSQL
        builder.Property(i => i.ImageUrls)
               .HasColumnType("jsonb");

        builder.HasOne(i => i.Category)
           .WithMany()
           .HasForeignKey(i => i.CategoryId)
           .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.RoomId);
        builder.HasIndex(i => i.ReporterId);
        builder.HasIndex(i => i.Status);
    }
}