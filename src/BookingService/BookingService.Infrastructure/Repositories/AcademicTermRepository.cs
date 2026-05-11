using BookingService.Application.Abtractions.Data;
using BookingService.Domain.ValueObjects;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Repositories;

public class AcademicTermRepository(BookingDbContext dbContext) : IAcademicTermRepository
{
    public async Task<AcademicTerm?> GetTermByNameAsync(string termName, CancellationToken cancellationToken)
    {
        var termData = await dbContext.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermName == termName, cancellationToken);

        if (termData == null) return null;

        return new AcademicTerm(
            termData.TermName, 
            termData.StartDate, 
            termData.EndDate, 
            termData.NumberOfMonths);
    }
}