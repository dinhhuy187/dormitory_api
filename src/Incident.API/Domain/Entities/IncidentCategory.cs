namespace Incident.API.Domain.Entities;

public class IncidentCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}