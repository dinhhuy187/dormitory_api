namespace BookingService.Infrastructure.Models;

public class AcademicTermData
{
    public Guid Id { get; set; }
    public string TermName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfMonths { get; set; }
}