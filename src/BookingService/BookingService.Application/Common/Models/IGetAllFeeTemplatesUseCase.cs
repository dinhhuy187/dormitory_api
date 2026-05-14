using BookingService.Application.Common;
using BookingService.Application.UseCases.FeeTemplates.Queries.GetAllFeeTemplates;

namespace BookingService.Application.Common.Models;

public interface IGetAllFeeTemplatesUseCase
{
    Task<Result<List<FeeTemplateResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}