using BookingService.Domain.SeedWork;

namespace BookingService.Domain.ValueObjects;

public record AcademicTerm
{
    public string TermName { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int NumberOfMonths { get; }
    public AcademicTerm(string termName, DateTime startDate, DateTime endDate, int numberOfMonths)
    {
        if (string.IsNullOrWhiteSpace(termName))
        {
            throw new DomainException("Term name cannot be empty.");
        }
        if (startDate.Date > endDate.Date)
        {
            throw new DomainException("Start date must be before end date.");
        }
        if (numberOfMonths <= 0)
        {
            throw new DomainException("Number of months must be greater than zero.");
        }

        TermName = termName;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        NumberOfMonths = numberOfMonths;
    }
}