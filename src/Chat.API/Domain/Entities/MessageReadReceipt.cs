namespace Chat.API.Domain.Entities;

public class MessageReadReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    public virtual Message? Message { get; set; }
}