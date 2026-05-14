namespace BookingService.Application.UseCases.FeeTemplates.Queries.GetAllFeeTemplates;

public record FeeTemplateResponse(
    Guid Id,
    string FeeCode,
    string FeeName,
    decimal Amount,
    string Description,
    bool IsMandatory,
    bool IsRefundable
);