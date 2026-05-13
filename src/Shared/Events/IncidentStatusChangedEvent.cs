namespace Shared.Events;

public record IncidentStatusChangedEvent(
    Guid IncidentId,
    Guid RoomId,
    string ReporterId,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt
);