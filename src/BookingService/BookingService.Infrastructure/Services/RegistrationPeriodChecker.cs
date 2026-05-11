using BookingService.Domain.Services;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Services;

public class RegistrationPeriodChecker(BookingDbContext dbContext) : IRegistrationPeriodChecker
{
    public async Task<bool> IsRegistrationPortalOpenAsync(string termName, CancellationToken cancellationToken)
    {
       var term = await dbContext.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermName == termName, cancellationToken);

        if (term == null)
        {
            return false; 
        }

        var now = DateTime.UtcNow;

        return now >= term.StartDate.AddDays(-20) && now <= term.StartDate;
    }
}