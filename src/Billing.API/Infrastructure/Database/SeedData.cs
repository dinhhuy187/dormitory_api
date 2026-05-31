using System.Net.Http.Json;
using Billing.API.Domain.Entities;
using Billing.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Billing.API.Infrastructure.Database;

public static class SeedData
{
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    public static async Task SeedAsync(
        BillingDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedServicePriceTiersAsync(dbContext, cancellationToken);
        var roomTypes = await GetRoomTypesFromRoomServiceAsync(httpClientFactory, logger, cancellationToken);
        await DeactivateLegacyContractTemplateAsync(dbContext, logger, cancellationToken);
        await SeedContractTemplatesAsync(dbContext, roomTypes, logger, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedServicePriceTiersAsync(BillingDbContext dbContext, CancellationToken cancellationToken)
    {
        var seedTiers = GetServicePriceTiers();
        var existingKeys = await dbContext.ServicePriceTiers
            .Select(t => new TierKey(t.ServiceType, t.RoomCapacity, t.EffectiveFrom, t.FromUsage))
            .ToListAsync(cancellationToken);

        var existingKeySet = existingKeys.ToHashSet();
        var missingTiers = seedTiers
            .Where(t => !existingKeySet.Contains(new TierKey(t.ServiceType, t.RoomCapacity, t.EffectiveFrom, t.FromUsage)))
            .ToList();

        if (missingTiers.Count > 0)
        {
            await dbContext.ServicePriceTiers.AddRangeAsync(missingTiers, cancellationToken);
        }
    }

    private static async Task SeedContractTemplatesAsync(
        BillingDbContext dbContext,
        IReadOnlyList<RoomTypeSyncDto> roomTypes,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var templates = GetContractTemplates(roomTypes, logger);
        var existingKeys = await dbContext.ContractTemplates
            .Select(t => new ContractTemplateKey(t.Code, t.Version))
            .ToListAsync(cancellationToken);

        var existingKeySet = existingKeys.ToHashSet();
        var missingTemplates = templates
            .Where(t => !existingKeySet.Contains(new ContractTemplateKey(t.Code, t.Version)))
            .ToList();

        if (missingTemplates.Count > 0)
        {
            await dbContext.ContractTemplates.AddRangeAsync(missingTemplates, cancellationToken);
        }
    }

    private static async Task DeactivateLegacyContractTemplateAsync(
        BillingDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var legacyTemplates = await dbContext.ContractTemplates
            .Where(template => template.Code == "STANDARD_DORM_CONTRACT" && template.IsActive)
            .ToListAsync(cancellationToken);

        if (legacyTemplates.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var template in legacyTemplates)
        {
            template.IsActive = false;
            template.UpdatedAt = now;
        }

        logger.LogInformation("Deactivated {TemplateCount} legacy STANDARD_DORM_CONTRACT templates.", legacyTemplates.Count);
    }

    private static IReadOnlyList<ServicePriceTier> GetServicePriceTiers()
    {
        List<ServicePriceTier> tiers = [];

        AddElectricityTiers(tiers, 8, [(0m, 100m), (101m, 200m), (201m, 400m), (401m, 600m), (601m, 800m), (801m, null)]);
        AddElectricityTiers(tiers, 7, [(0m, 87.5m), (87.6m, 175m), (176m, 350m), (351m, 525m), (526m, 700m), (701m, null)]);
        AddElectricityTiers(tiers, 6, [(0m, 75m), (76m, 150m), (151m, 300m), (301m, 450m), (451m, 600m), (601m, null)]);
        AddElectricityTiers(tiers, 5, [(0m, 62.5m), (62.6m, 125m), (126m, 250m), (251m, 375m), (376m, 500m), (501m, null)]);
        AddElectricityTiers(tiers, 4, [(0m, 50m), (51m, 100m), (101m, 200m), (201m, 300m), (301m, 400m), (401m, null)]);
        AddElectricityTiers(tiers, 3, [(0m, 37.5m), (37.6m, 75m), (76m, 150m), (151m, 225m), (226m, 300m), (301m, null)]);
        AddElectricityTiers(tiers, 2, [(0m, 25m), (26m, 50m), (51m, 100m), (101m, 150m), (151m, 200m), (201m, null)]);
        AddElectricityTiers(tiers, 1, [(0m, 12.5m), (12.6m, 25m), (26m, 50m), (51m, 75m), (76m, 100m), (101m, null)]);

        AddWaterTiers(tiers, 8, [(0m, 32m), (32.1m, 48m), (48.1m, null)]);
        AddWaterTiers(tiers, 7, [(0m, 28m), (28.1m, 42m), (42.1m, null)]);
        AddWaterTiers(tiers, 6, [(0m, 24m), (24.1m, 36m), (36.1m, null)]);
        AddWaterTiers(tiers, 5, [(0m, 20m), (20.1m, 30m), (30.1m, null)]);
        AddWaterTiers(tiers, 4, [(0m, 16m), (16.1m, 24m), (24.1m, null)]);
        AddWaterTiers(tiers, 3, [(0m, 12m), (12.1m, 18m), (18.1m, null)]);
        AddWaterTiers(tiers, 2, [(0m, 8m), (8.1m, 12m), (12.1m, null)]);
        AddWaterTiers(tiers, 1, [(0m, 4m), (4.1m, 6m), (6.1m, null)]);

        return tiers;
    }

    private static void AddElectricityTiers(List<ServicePriceTier> tiers, int roomCapacity, IReadOnlyList<(decimal From, decimal? To)> ranges)
    {
        decimal[] prices = [1984m, 2050m, 2380m, 2998m, 3350m, 3460m];

        for (var i = 0; i < ranges.Count; i++)
        {
            tiers.Add(CreateTier(ServiceType.Electricity, roomCapacity, i + 1, ranges[i].From, ranges[i].To, "kWh", prices[i]));
        }
    }

    private static void AddWaterTiers(List<ServicePriceTier> tiers, int roomCapacity, IReadOnlyList<(decimal From, decimal? To)> ranges)
    {
        decimal[] prices = [5485m, 10557m, 11799m];

        for (var i = 0; i < ranges.Count; i++)
        {
            tiers.Add(CreateTier(ServiceType.Water, roomCapacity, i + 1, ranges[i].From, ranges[i].To, "m3", prices[i]));
        }
    }

    private static ServicePriceTier CreateTier(
        ServiceType serviceType,
        int roomCapacity,
        int tierNumber,
        decimal fromUsage,
        decimal? toUsage,
        string unitName,
        decimal unitPrice)
    {
        var now = DateTime.UtcNow;

        return new ServicePriceTier
        {
            ServiceType = serviceType,
            RoomCapacity = roomCapacity,
            TierName = $"Bac {tierNumber}",
            FromUsage = fromUsage,
            ToUsage = toUsage,
            UnitName = unitName,
            UnitPrice = unitPrice,
            EffectiveFrom = EffectiveFrom,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static async Task<IReadOnlyList<RoomTypeSyncDto>> GetRoomTypesFromRoomServiceAsync(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("RoomServiceClient");
            var roomTypes = await client.GetFromJsonAsync<List<RoomTypeSyncDto>>(
                "/api/rooms/roomtypes/sync",
                cancellationToken);

            if (roomTypes is null || roomTypes.Count == 0)
            {
                logger.LogWarning("RoomService room type sync returned no data. Billing will seed only the generic contract template.");
                return [];
            }

            logger.LogInformation("Synced {RoomTypeCount} room types from RoomService for Billing contract template seed.", roomTypes.Count);
            return roomTypes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync room types from RoomService for Billing contract template seed.");
            return [];
        }
    }

    private static IReadOnlyList<ContractTemplate> GetContractTemplates(
        IReadOnlyList<RoomTypeSyncDto> roomTypes,
        ILogger logger)
    {
        var now = DateTime.UtcNow;
        var templates = new List<ContractTemplate>
        {
            CreateFallbackContractTemplate(now)
        };

        AddRoomTypeTemplate(templates, roomTypes, logger, now, "DORM_CONTRACT_STANDARD_8", "Mẫu hợp đồng phòng 8 sinh viên tiêu chuẩn", 8, 230000m);
        AddRoomTypeTemplate(templates, roomTypes, logger, now, "DORM_CONTRACT_STANDARD_6", "Mẫu hợp đồng phòng 6 sinh viên tiêu chuẩn", 6, 310000m);
        AddRoomTypeTemplate(templates, roomTypes, logger, now, "DORM_CONTRACT_SERVICE_6_AC", "Mẫu hợp đồng phòng 6 sinh viên dịch vụ có máy lạnh", 6, 550000m);
        AddRoomTypeTemplate(templates, roomTypes, logger, now, "DORM_CONTRACT_SERVICE_4_BASIC", "Mẫu hợp đồng phòng 4 sinh viên dịch vụ cơ bản", 4, 950000m);
        AddRoomTypeTemplate(templates, roomTypes, logger, now, "DORM_CONTRACT_SERVICE_4_FULL", "Mẫu hợp đồng phòng 4 sinh viên dịch vụ đầy đủ tiện ích", 4, 1370000m);
        AddRoomTypeTemplate(templates, roomTypes, logger, now, "DORM_CONTRACT_SERVICE_2_VIP", "Mẫu hợp đồng phòng 2 sinh viên dịch vụ VIP", 2, 3140000m);

        return templates;
    }

    private static void AddRoomTypeTemplate(
        List<ContractTemplate> templates,
        IReadOnlyList<RoomTypeSyncDto> roomTypes,
        ILogger logger,
        DateTime now,
        string code,
        string name,
        int capacity,
        decimal basePrice)
    {
        var roomType = FindRoomType(roomTypes, capacity, basePrice);
        if (roomType is null)
        {
            logger.LogWarning(
                "Skipping Billing contract template {TemplateCode}; RoomService room type was not found for capacity {Capacity} and base price {BasePrice}.",
                code,
                capacity,
                basePrice);
            return;
        }

        templates.Add(new ContractTemplate
        {
            Code = code,
            Name = name,
            RoomTypeId = roomType.Id,
            Version = 1,
            Content = BuildContractContent(roomType, isFallback: false),
            IsActive = true,
            EffectiveFrom = EffectiveFrom,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private static ContractTemplate CreateFallbackContractTemplate(DateTime now)
    {
        var fallbackRoomType = new RoomTypeSyncDto(
            Guid.Empty,
            "Loai phong noi tru",
            0,
            0,
            []);

        return new ContractTemplate
        {
            Code = "DORM_CONTRACT_FALLBACK",
            Name = "Mẫu hợp đồng nội trú mặc định",
            RoomTypeId = null,
            Version = 1,
            Content = BuildContractContent(fallbackRoomType, isFallback: true),
            IsActive = true,
            EffectiveFrom = EffectiveFrom,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static RoomTypeSyncDto? FindRoomType(
        IReadOnlyList<RoomTypeSyncDto> roomTypes,
        int capacity,
        decimal basePrice)
    {
        return roomTypes.FirstOrDefault(roomType =>
            roomType.Capacity == capacity &&
            roomType.BasePrice == basePrice);
    }

    private static string BuildContractContent(RoomTypeSyncDto roomType, bool isFallback)
    {
        var roomTypeName = isFallback ? "Loại phòng sẽ được xác định theo phân công thực tế" : roomType.Name;
        var capacityText = isFallback ? "Theo loại phòng được xếp" : $"{roomType.Capacity} sinh viên";
        var basePriceText = isFallback ? "Theo bảng giá phòng đang áp dụng" : $"{roomType.BasePrice:N0} VND/tháng";
        var amenitiesText = roomType.Amenities.Count == 0
            ? "Tiện ích theo tiêu chuẩn ký túc xá."
            : string.Join(", ", roomType.Amenities);
        var categoryText = InferRoomTypeCategory(roomType, isFallback);

        return $$"""
                 # Mẫu hợp đồng nội trú ký túc xá

                 ## 1. Thông tin loại phòng
                 Loại phòng áp dụng: {{roomTypeName}}.
                 Sức chứa: {{capacityText}}.
                 Nhóm phòng: {{categoryText}}.
                 Tiện ích: {{amenitiesText}}.

                 ## 2. Thời hạn lưu trú
                 Thời hạn lưu trú được xác định theo đợt đăng ký, quyết định phân phòng và lịch đào tạo của nhà trường. Sinh viên chỉ được sử dụng đúng phòng được phân công.

                 ## 3. Phí phòng hằng tháng
                 Phí phòng hằng tháng: {{basePriceText}}. Mức phí có thể được cập nhật theo bảng giá được công bố trước kỳ thanh toán tiếp theo.

                 ## 4. Điện, nước và các khoản phụ thu
                 Điện và nước được tính từ chỉ số đồng hồ của phòng. Tiền điện áp dụng bậc giá Billing đang hiệu lực và cộng VAT 8%. Tiền nước áp dụng bậc giá Billing đang hiệu lực, đã bao gồm phí và thuế theo cấu hình. Các phụ thu hợp lệ được thể hiện trên hóa đơn hằng tháng.

                 ## 5. Đặt cọc và bồi thường tài sản
                 Sinh viên có trách nhiệm giữ gìn tài sản trong phòng và khu vực dùng chung. Thiệt hại do sử dụng sai quy định phải được bồi thường theo biên bản kiểm tra và mức chi phí thực tế.

                 ## 6. Quyền và nghĩa vụ của sinh viên
                 Sinh viên được sử dụng chỗ ở, tiện ích và dịch vụ hỗ trợ theo loại phòng. Sinh viên phải thanh toán hóa đơn đúng hạn, bảo quản tài sản, chấp hành nội quy và thông báo kịp thời các sự cố về điện, nước, an toàn hoặc tài sản.

                 ## 7. Nội quy sinh hoạt
                 Sinh viên phải giữ vệ sinh, bảo đảm trật tự, không tự ý chuyển phòng, không cho người khác lưu trú trái phép và không cài đặt thiết bị làm thay đổi kết cấu phòng khi chưa được phê duyệt.

                 ## 8. Chấm dứt hoặc hủy hiệu lực hợp đồng
                 Hợp đồng có thể chấm dứt khi hết thời hạn, sinh viên trả phòng theo quy trình, không còn đủ điều kiện lưu trú hoặc vi phạm nghiêm trọng nội quy ký túc xá. Các khoản nợ, bồi thường và tài sản bàn giao phải được xử lý trước khi hoàn tất trả phòng.

                 ## 9. Điều khoản chung
                 Hóa đơn hằng tháng do Billing phát hành. Sinh viên phải thanh toán trước hạn thanh toán ký túc xá được cấu hình. Các nội dung chưa quy định trong mẫu này được áp dụng theo quy chế ký túc xá và thông báo hợp lệ của đơn vị quản lý.
                 """;
    }

    private static string InferRoomTypeCategory(RoomTypeSyncDto roomType, bool isFallback)
    {
        if (isFallback)
        {
            return "Mặc định";
        }

        var normalizedName = roomType.Name.ToLowerInvariant();
        if (normalizedName.Contains("vip") || roomType.BasePrice >= 3000000m)
        {
            return "VIP";
        }

        if (normalizedName.Contains("full") || roomType.BasePrice >= 1300000m)
        {
            return "Dịch vụ đầy đủ tiện ích";
        }

        if (normalizedName.Contains("may lanh") ||
            normalizedName.Contains("air") ||
            roomType.BasePrice == 550000m)
        {
            return "Dịch vụ có máy lạnh";
        }

        if (roomType.BasePrice >= 900000m)
        {
            return "Dịch vụ cơ bản";
        }

        return "Tiêu chuẩn";
    }

    private sealed record RoomTypeSyncDto(
        Guid Id,
        string Name,
        int Capacity,
        decimal BasePrice,
        List<string> Amenities);

    private readonly record struct TierKey(ServiceType ServiceType, int? RoomCapacity, DateOnly EffectiveFrom, decimal FromUsage);

    private readonly record struct ContractTemplateKey(string Code, int Version);
}
