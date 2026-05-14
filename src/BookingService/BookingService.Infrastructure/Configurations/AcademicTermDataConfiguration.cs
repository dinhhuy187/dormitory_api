using BookingService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Configurations;

public class AcademicTermDataConfiguration : IEntityTypeConfiguration<AcademicTermData>
{
    public void Configure(EntityTypeBuilder<AcademicTermData> builder)
    {
        builder.ToTable("AcademicTerms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TermName).HasMaxLength(100);
    }
}