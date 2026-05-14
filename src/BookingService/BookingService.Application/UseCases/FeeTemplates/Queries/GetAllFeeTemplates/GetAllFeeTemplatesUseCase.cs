using BookingService.Application.Common;
using BookingService.Application.Common.Models;
using BookingService.Domain.Repositories;

namespace BookingService.Application.UseCases.FeeTemplates.Queries.GetAllFeeTemplates;

public class GetAllFeeTemplatesUseCase(IFeeTemplateRepository feeTemplateRepository) : IGetAllFeeTemplatesUseCase
{
    public async Task<Result<List<FeeTemplateResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var feeTemplates = await feeTemplateRepository.GetAllAsync(cancellationToken);

            var items = feeTemplates.Select(f => new FeeTemplateResponse(
                Id: f.Id,
                FeeCode: f.FeeCode,
                FeeName: f.FeeName,
                Amount: f.Amount,
                Description: f.Description,
                IsMandatory: f.IsMandatory,
                IsRefundable: f.IsRefundable
            )).ToList();

            return Result<List<FeeTemplateResponse>>.Success(items);
        }
        catch (Exception ex)
        {
            return Result<List<FeeTemplateResponse>>.Failure(ex.Message);
        }
    }
}