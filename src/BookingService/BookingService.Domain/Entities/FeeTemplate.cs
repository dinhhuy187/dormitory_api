namespace BookingService.Domain.Entities;

public class FeeTemplate
{
    public Guid Id { get; set; }
    public string FeeCode { get; set; } = string.Empty;
    public string FeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool IsRefundable { get; set; }
}