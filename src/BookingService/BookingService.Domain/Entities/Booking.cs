using BookingService.Domain.Enums;
using BookingService.Domain.SeedWork;
using BookingService.Domain.Services;
using BookingService.Domain.ValueObjects;
using BookingService.Domain.Events;

namespace BookingService.Domain.Entities;

public class Booking : Entity, IAggregateRoot
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid UserId { get; private set; }
    
    public AcademicTerm Term { get; private set; } 
    
    public decimal PricePerMonth { get; private set; }
    public decimal BasePrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<BookingFee> _fees = [];
    public IReadOnlyCollection<BookingFee> Fees => _fees.AsReadOnly();

    private Booking() { }

    private Booking(
        Guid id, 
        Guid roomId, 
        Guid userId, 
        AcademicTerm term, 
        decimal pricePerMonth)
    {
        Id = id;
        RoomId = roomId;
        UserId = userId;
        Term = term;
        
        PricePerMonth = pricePerMonth;
        BasePrice = pricePerMonth * term.NumberOfMonths; 
        
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        RecalculateTotalPrice();
    }

    public static async Task<Booking> CreateAsync(
        Guid roomId, 
        Guid userId, 
        AcademicTerm term,
        decimal pricePerMonth,
        IBookingRulesChecker rulesChecker,
        IRegistrationPeriodChecker portalChecker,
        CancellationToken ct)
    {
        // 1. Kiểm tra cổng đăng ký có đang mở không?
        var isPortalOpen = await portalChecker.IsRegistrationPortalOpenAsync(term.TermName, ct);
        if (!isPortalOpen)
            throw new DomainException($"Cổng đăng ký cho {term.TermName} hiện đang đóng. Bạn không thể đặt phòng lúc này.");

        // 2. Kiểm tra giới hạn số lượng phòng của sinh viên
        var hasExceededLimit = await rulesChecker.HasExceededActiveBookingLimitAsync(userId, term.TermName, ct);
        if (hasExceededLimit)
            throw new DomainException("Bạn đã đạt giới hạn số lượng phòng được phép đặt.");
        
        var isRoomAvailable = await rulesChecker.IsRoomAvailableForBookingAsync(roomId, ct);
        if (!isRoomAvailable)
            throw new DomainException("Phòng bạn chọn không tồn tại, đang bảo trì hoặc đã kín chỗ.");

        var booking = new Booking(Guid.NewGuid(), roomId, userId, term, pricePerMonth);

        booking.AddDomainEvent(new BookingCreatedDomainEvent(booking.Id, booking.RoomId, booking.UserId));

        return booking;
    }

    public void AddFee(string feeName, decimal amount, bool isRefundable = false)
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException("Chỉ có thể thêm phụ phí khi đơn đặt phòng đang ở trạng thái chờ (Pending).");

        if (amount < 0)
            throw new DomainException("Số tiền phụ phí không được âm.");

        var fee = new BookingFee(feeName, amount, isRefundable);
        _fees.Add(fee);

        RecalculateTotalPrice();
    }

    public void RemoveFee(Guid feeId)
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException("Chỉ có thể xóa phụ phí khi đơn đặt phòng đang ở trạng thái chờ (Pending).");

        var fee = _fees.FirstOrDefault(f => f.Id == feeId);
        if (fee != null)
        {
            _fees.Remove(fee);
            RecalculateTotalPrice();
        }
    }

    private void RecalculateTotalPrice()
    {
        // Tổng tiền = Tiền phòng + Tất cả các phụ phí (BHYT, Phí hồ sơ, Thế chân...)
        TotalPrice = BasePrice + _fees.Sum(f => f.Amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException("Chỉ có thể xác nhận đơn đặt phòng đang ở trạng thái Pending.");

        Status = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookingConfirmedDomainEvent(Id, RoomId));
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Completed || Status == BookingStatus.Active)
            throw new DomainException("Không thể hủy phòng khi sinh viên đã nhận phòng hoặc đã hoàn thành.");

        if (Status == BookingStatus.Canceled)
            return; 

        Status = BookingStatus.Canceled;
        UpdatedAt = DateTime.UtcNow;

        // Bắn sự kiện để Room Service "nhả" lại chỗ trống nếu trước đó đã Confirmed
        AddDomainEvent(new BookingCanceledDomainEvent(Id, RoomId));
    }

    public void CheckIn()
    {
        if (Status != BookingStatus.Confirmed)
            throw new DomainException("Sinh viên phải hoàn tất thanh toán (Confirmed) mới được nhận phòng.");

        if (DateTime.UtcNow.Date < Term.StartDate.Date)
            throw new DomainException("Chưa đến ngày nhận phòng theo lịch trình.");

        Status = BookingStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new StudentCheckedInDomainEvent(Id, RoomId, UserId));
    }

    public void CheckOut()
    {
        if (Status != BookingStatus.Active)
            throw new DomainException("Sinh viên chưa nhận phòng thì không thể trả phòng.");

        Status = BookingStatus.Completed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new StudentCheckedOutDomainEvent(Id, RoomId));
    }
}