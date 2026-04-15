using BookingService.Domain.SeedWork;

namespace BookingService.Domain.Entities;

public class BookingFee : Entity
{
    public Guid Id { get; private set; }
    public string FeeName { get; private set; } // VD: "Tiền thế chân", "BHYT", "Phí hồ sơ"
    public decimal Amount { get; private set; }
    public bool IsRefundable { get; private set; } // True nếu là tiền thế chân (được hoàn lại khi trả phòng)

    // Constructor nội bộ, chỉ cho phép Booking (Aggregate Root) khởi tạo nó
    internal BookingFee(string feeName, decimal amount, bool isRefundable)
    {
        Id = Guid.NewGuid();
        FeeName = feeName;
        Amount = amount;
        IsRefundable = isRefundable;
    }
}