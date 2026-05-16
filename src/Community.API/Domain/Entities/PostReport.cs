using Community.API.Domain.Enums;

namespace Community.API.Domain.Entities;

public class PostReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public ReportReason Reason { get; set; }
    public string? Note { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Post? Post { get; set; }
}