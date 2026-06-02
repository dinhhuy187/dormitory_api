using BookingService.Infrastructure.Sagas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Configurations;

public class BookingSagaStateConfiguration : IEntityTypeConfiguration<BookingSagaState>
{
    public void Configure(EntityTypeBuilder<BookingSagaState> builder)
    {
        builder.ToTable("BookingSagaStates");

        builder.HasKey(state => state.CorrelationId);
        builder.Property(state => state.CurrentState).HasMaxLength(64).IsRequired();
        builder.Property(state => state.TermName).HasMaxLength(100).IsRequired();
        builder.Property(state => state.FeesJson).HasColumnType("jsonb").IsRequired();
    }
}
