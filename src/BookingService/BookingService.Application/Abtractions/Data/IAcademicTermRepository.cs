using BookingService.Domain.ValueObjects;

namespace BookingService.Application.Abtractions.Data;

public interface IAcademicTermRepository
{
    Task<AcademicTerm> GetTermByNameAsync(string termName, CancellationToken cancellationToken);    
}