using MassTransit;

namespace BookingService.Infrastructure.Sagas;

public class BookingSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } 
    public string CurrentState { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid RoomId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfMonths { get; set; }
    public decimal PricePerMonth { get; set; }
    public decimal BasePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PaymentDueAt { get; set; }
    public Guid? InvoiceId { get; set; }
    public string FeesJson { get; set; } = "[]";
}

public class BookingInvoiceFeeSnapshot
{
    public string FeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsRefundable { get; set; }
}
