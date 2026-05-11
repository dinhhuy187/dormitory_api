using Incident.API.Domain.Enums;

namespace Incident.API.Domain.Entities;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoomId { get; set; }
    public string ReporterId { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }
    public virtual IncidentCategory? Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = [];
    public IncidentStatus Status { get; set; } = IncidentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}