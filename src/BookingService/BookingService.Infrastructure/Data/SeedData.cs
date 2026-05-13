using System.Net.Http.Json;
using BookingService.Domain.Entities;
using BookingService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.Data;

public class SeedData
{
    private record RoomSyncDto(
        Guid Id, 
        string RoomName, 
        decimal MonthlyPrice, 
        int Capacity, 
        int OccupiedCount, 
        string Status);
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedData>>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        // 1. Kiểm tra xem BookingDB đã có dữ liệu phòng chưa
        if (await dbContext.Rooms.AnyAsync() || await dbContext.AcademicTerms.AnyAsync())
        {
            logger.LogInformation("Bảng RoomData, AcademicTerms đã có dữ liệu. Bỏ qua đồng bộ.");
            return;
        }

        logger.LogInformation("Bắt đầu đồng bộ dữ liệu phòng từ Room Service...");

        try
        {
            // 2. Gọi API từ Room Service
            var client = httpClientFactory.CreateClient("RoomServiceClient");
            var sourceRooms = await client.GetFromJsonAsync<List<RoomSyncDto>>("/api/rooms/sync");

            if (sourceRooms != null && sourceRooms.Count != 0)
            {
                // 3. Mapping: Chuyển DTO thành RoomData Entity của Booking Service
                var roomDatas = sourceRooms.Select(dto => new RoomData
                {
                    Id = dto.Id,
                    RoomName = dto.RoomName,
                    MonthlyPrice = dto.MonthlyPrice,
                    Capacity = dto.Capacity,
                    OccupiedCount = dto.OccupiedCount,
                    Status = dto.Status
                }).ToList();

                // 4. Lưu vào Database của Booking Service
                await dbContext.Rooms.AddRangeAsync(roomDatas);

                DateTime CreateUtcDate(int year, int month, int day) => DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);

                var terms = new List<AcademicTermData>
                {
                    new AcademicTermData 
                    { 
                        TermName = "HK1_2025_2026", 
                        StartDate = CreateUtcDate(2025, 9, 1), 
                        EndDate = CreateUtcDate(2026, 1, 31), 
                        NumberOfMonths = 5 
                    },
                    
                    new AcademicTermData 
                    { 
                        TermName = "HK2_2025_2026", 
                        StartDate = CreateUtcDate(2026, 2, 1), 
                        EndDate = CreateUtcDate(2026, 6, 30), 
                        NumberOfMonths = 5 
                    },
                    new AcademicTermData 
                    { 
                        TermName = "HKH_2025_2026", 
                        StartDate = CreateUtcDate(2026, 7, 1), 
                        EndDate = CreateUtcDate(2026, 8, 31), 
                        NumberOfMonths = 2 
                    },
                    
                    new AcademicTermData 
                    { 
                        TermName = "HK1_2026_2027", 
                        StartDate = CreateUtcDate(2026, 9, 1), 
                        EndDate = CreateUtcDate(2027, 1, 31), 
                        NumberOfMonths = 5 
                    },
                    new AcademicTermData 
                    { 
                        TermName = "HK2_2026_2027", 
                        StartDate = CreateUtcDate(2027, 2, 1), 
                        EndDate = CreateUtcDate(2027, 6, 30), 
                        NumberOfMonths = 5 
                    }
                };

                await dbContext.AcademicTerms.AddRangeAsync(terms);

                var fees = new List<FeeTemplate>
                {
                    new FeeTemplate 
                    { 
                        FeeCode = "HO_SO", 
                        FeeName = "Tiền hồ sơ đăng ký KTX", 
                        Amount = 60000, 
                        IsMandatory = true, 
                        Description = "Đóng 1 lần khi làm thủ tục nhập trú.",
                        IsRefundable = false
                    },
                    new FeeTemplate 
                    { 
                        FeeCode = "THE_CHAN", 
                        FeeName = "Tiền thế chân tài sản, CSVC", 
                        Amount = 100000, 
                        IsMandatory = true, 
                        Description = "Hoàn trả khi sinh viên trả phòng và không làm hư hỏng tài sản.",
                        IsRefundable = true
                    },
                    new FeeTemplate 
                    { 
                        FeeCode = "BHTN_12T", 
                        FeeName = "Bảo hiểm tai nạn (12 tháng)", 
                        Amount = 30000, 
                        IsMandatory = true, 
                        Description = "Bảo hiểm tai nạn bắt buộc theo quy định.",
                        IsRefundable = false
                    },
                    new FeeTemplate 
                    { 
                        FeeCode = "BHYT_12T", 
                        FeeName = "Bảo hiểm Y tế (12 tháng)", 
                        Amount = 680400, 
                        IsMandatory = false, // Có thể false vì sinh viên có thể đã mua ở địa phương
                        Description = "BHYT HSSV (Thu hộ). Sinh viên có thể kê khai mã thẻ nếu đã mua.",
                        IsRefundable = false
                    },
                    new FeeTemplate 
                    { 
                        FeeCode = "BHYT_15T", 
                        FeeName = "Bảo hiểm Y tế (15 tháng - Dành cho Tân SV)", 
                        Amount = 850500, 
                        IsMandatory = false, 
                        Description = "BHYT HSSV (Thu hộ) bao gồm các tháng chờ.",
                        IsRefundable = false
                    }
                };
                await dbContext.FeeTemplates.AddRangeAsync(fees);
                
                await dbContext.SaveChangesAsync();

                logger.LogInformation($"Đồng bộ thành công {roomDatas.Count} phòng!");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi đồng bộ dữ liệu phòng từ Room Service.");
        }
    }
}