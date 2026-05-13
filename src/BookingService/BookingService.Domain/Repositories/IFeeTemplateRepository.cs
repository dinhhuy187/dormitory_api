using BookingService.Domain.Entities;

namespace BookingService.Domain.Repositories;

public interface IFeeTemplateRepository
{
    Task<IEnumerable<FeeTemplate>> GetMandatoryFeesAsync(CancellationToken cancellationToken = default);
    
    // Lấy các loại phí theo danh sách mã (dùng cho các phí tùy chọn)
    Task<IEnumerable<FeeTemplate>> GetFeesByCodesAsync(IEnumerable<string> feeCodes, CancellationToken cancellationToken = default);
}