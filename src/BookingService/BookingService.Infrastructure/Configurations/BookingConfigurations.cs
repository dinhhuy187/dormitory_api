using BookingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingService.Infrastructure.Configurations;

public class BookingConfigurations : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);

        builder.Ignore(b => b.DomainEvents);

        // Ánh xạ Value Object (AcademicTerm) thành các cột trên cùng bảng Bookings
        builder.OwnsOne(b => b.Term, termBuilder =>
        {
            termBuilder.Property(t => t.TermName).HasColumnName("TermName").HasMaxLength(100).IsRequired();
            termBuilder.Property(t => t.StartDate).HasColumnName("StartDate").IsRequired();
            termBuilder.Property(t => t.EndDate).HasColumnName("EndDate").IsRequired();
            termBuilder.Property(t => t.NumberOfMonths).HasColumnName("NumberOfMonths").IsRequired();
        });

        // 4. Cấu hình danh sách Phụ phí (One-to-Many) với Private Backing Field
        builder.HasMany(b => b.Fees)
               .WithOne() // Không cần navigation property ngược lại từ BookingFee lên Booking
               .HasForeignKey("BookingId") // Cột ẩn (Shadow property) trong bảng BookingFees
               .OnDelete(DeleteBehavior.Cascade);

        // NÓI CHO EF CORE BIẾT: Hãy map dữ liệu vào biến ẩn _fees thay vì property Fees (vì Fees chỉ cho đọc)
        builder.Metadata.FindNavigation(nameof(Booking.Fees))!
               .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class BookingFeeConfiguration : IEntityTypeConfiguration<BookingFee>
{
    public void Configure(EntityTypeBuilder<BookingFee> builder)
    {
        builder.ToTable("BookingFees");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FeeName).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Amount).HasColumnType("numeric(18,2)");
    }
}