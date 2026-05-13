using BookingService.Domain.Entities;
using BookingService.Domain.Repositories;
using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Repositories;

public class FeeTemplateRepository(BookingDbContext dbContext) : IFeeTemplateRepository
{
    public async Task<IEnumerable<FeeTemplate>> GetFeesByCodesAsync(IEnumerable<string> feeCodes, CancellationToken cancellationToken = default)
    {
        if (feeCodes == null || !feeCodes.Any())
        {
            return Enumerable.Empty<FeeTemplate>();
        }
        var fees = await dbContext.FeeTemplates
            .AsNoTracking()
            .Where(f => feeCodes.Contains(f.FeeCode))
            .ToListAsync(cancellationToken);
        return fees;
    }

    public async Task<IEnumerable<FeeTemplate>> GetMandatoryFeesAsync(CancellationToken cancellationToken = default)
    {
        var mandatoryFees = await dbContext.FeeTemplates
            .AsNoTracking()
            .Where(f => f.IsMandatory)
            .ToListAsync(cancellationToken);
        return mandatoryFees;
    }
}