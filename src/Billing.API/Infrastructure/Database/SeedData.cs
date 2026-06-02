using System.Net.Http.Json;
using System.Text.Json;
using Billing.API.Domain.Entities;
using Billing.API.Domain.Enums;
using Billing.API.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Billing.API.Infrastructure.Database;

public static class SeedData
{
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    public static async Task SeedAsync(
        BillingDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IRoomBillingClient roomBillingClient,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await SeedServicePriceTiersAsync(dbContext, cancellationToken);
        var roomTypes = await GetRoomTypesFromRoomServiceAsync(httpClientFactory, logger, cancellationToken);
        await DeactivateLegacyContractTemplateAsync(dbContext, logger, cancellationToken);
        await SeedContractTemplatesAsync(dbContext, roomTypes, logger, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var bookings = await GetBookingsFromBookingServiceAsync(httpClientFactory, logger, cancellationToken);
        await SeedBookingRegistrationInvoicesAsync(dbContext, bookings, roomBillingClient, logger, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedMonthlyUtilityInvoicesAndSurchargesAsync(dbContext, bookings, roomBillingClient, logger, cancellationToken);
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

    private static async Task<IReadOnlyList<BookingBillingSyncDto>> GetBookingsFromBookingServiceAsync(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 60;
        var client = httpClientFactory.CreateClient("BookingServiceClient");

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var bookings = await client.GetFromJsonAsync<List<BookingBillingSyncDto>>(
                    "/api/bookings/sync",
                    cancellationToken);

                if (bookings is { Count: > 0 })
                {
                    logger.LogInformation("Synced {BookingCount} bookings from BookingService for Billing invoice seed.", bookings.Count);
                    return bookings;
                }

                logger.LogWarning(
                    "BookingService sync returned no eligible bookings on attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    maxAttempts);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to sync bookings from BookingService on attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    maxAttempts);
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        logger.LogError("BookingService sync did not return eligible bookings after {MaxAttempts} attempts. Billing invoice seed will be skipped.", maxAttempts);
        return [];
    }

    private static async Task SeedBookingRegistrationInvoicesAsync(
        BillingDbContext dbContext,
        IReadOnlyList<BookingBillingSyncDto> bookings,
        IRoomBillingClient roomBillingClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var eligibleBookings = GetEligibleBookings(bookings);
        if (eligibleBookings.Count == 0)
        {
            logger.LogInformation("No confirmed or active bookings were available for Billing booking-registration invoice seed.");
            return;
        }

        var bookingIds = eligibleBookings
            .Select(booking => booking.BookingId)
            .ToList();

        var existingBookingInvoiceIds = await dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceType == InvoiceType.BookingRegistration &&
                              invoice.BookingId.HasValue &&
                              bookingIds.Contains(invoice.BookingId.Value))
            .Select(invoice => invoice.BookingId!.Value)
            .ToListAsync(cancellationToken);

        var existingBookingInvoiceIdSet = existingBookingInvoiceIds.ToHashSet();
        var roomCache = new Dictionary<Guid, RoomBillingInfo?>();
        var createdCount = 0;
        var skippedCount = 0;

        foreach (var booking in eligibleBookings)
        {
            if (existingBookingInvoiceIdSet.Contains(booking.BookingId))
            {
                skippedCount++;
                continue;
            }

            if (!roomCache.TryGetValue(booking.RoomId, out var room))
            {
                room = await GetRoomBillingInfoForSeedAsync(roomBillingClient, booking.RoomId, logger, cancellationToken);
                roomCache[booking.RoomId] = room;
            }

            if (room is null)
            {
                skippedCount++;
                logger.LogWarning("Skipped booking invoice for booking {BookingId}; room detail unavailable.", booking.BookingId);
                continue;
            }

            var surchargeLines = BuildBookingRegistrationSurchargeLines(booking).ToList();
            var surchargeTotal = surchargeLines.Sum(line => line.Amount);
            if (surchargeTotal != booking.TotalPrice)
            {
                skippedCount++;
                logger.LogWarning(
                    "Skipped booking invoice for booking {BookingId}; surcharge total {SurchargeTotal} does not match booking total {BookingTotal}.",
                    booking.BookingId,
                    surchargeTotal,
                    booking.TotalPrice);
                continue;
            }

            var createdAt = EnsureUtc(booking.CreatedAt);
            var paidAt = EnsureUtc(createdAt.AddHours(24));
            var invoice = new Invoice
            {
                InvoiceType = InvoiceType.BookingRegistration,
                BookingId = booking.BookingId,
                RoomId = booking.RoomId,
                BuildingCode = room.BuildingCode.Trim(),
                Floor = room.Floor,
                StudentId = booking.StudentId,
                TermName = booking.TermName,
                DueAt = EnsureUtc(booking.PaymentDueAt),
                Description = $"Booking registration invoice for {booking.TermName}",
                BillingMonth = (short)createdAt.Month,
                BillingYear = createdAt.Year,
                ElectricityOldIndex = 0,
                ElectricityNewIndex = 0,
                ElectricityUsage = 0,
                ElectricityAmount = 0m,
                WaterOldIndex = 0,
                WaterNewIndex = 0,
                WaterUsage = 0,
                WaterAmount = 0m,
                SurchargeTotal = surchargeTotal,
                TotalAmount = booking.TotalPrice,
                Status = InvoiceStatus.Paid,
                PaidAt = paidAt,
                CreatedAt = createdAt,
                UpdatedAt = paidAt
            };

            foreach (var line in surchargeLines)
            {
                invoice.Surcharges.Add(new Surcharge
                {
                    Name = line.Name,
                    Amount = line.Amount,
                    CreatedAt = createdAt
                });
            }

            dbContext.Invoices.Add(invoice);
            existingBookingInvoiceIdSet.Add(booking.BookingId);
            createdCount++;
        }

        logger.LogInformation(
            "Billing booking-registration invoice seed completed. Created {CreatedCount} invoices and skipped {SkippedCount} bookings.",
            createdCount,
            skippedCount);
    }

    private static async Task SeedMonthlyUtilityInvoicesAndSurchargesAsync(
        BillingDbContext dbContext,
        IReadOnlyList<BookingBillingSyncDto> bookings,
        IRoomBillingClient roomBillingClient,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var eligibleBookings = GetEligibleBookings(bookings);

        if (eligibleBookings.Count == 0)
        {
            logger.LogInformation("No confirmed or active bookings were available for Billing invoice seed.");
            return;
        }

        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var candidates = eligibleBookings
            .SelectMany(booking => GetCandidateMonths(booking, currentMonth)
                .Select(period => new SeedInvoiceCandidate(booking, period.Year, period.Month)))
            .GroupBy(candidate => new InvoiceSeedKey(candidate.Booking.RoomId, candidate.Year, candidate.Month))
            .Select(group => group.First())
            .OrderBy(candidate => candidate.Booking.RoomId)
            .ThenBy(candidate => candidate.Year)
            .ThenBy(candidate => candidate.Month)
            .ToList();

        if (candidates.Count == 0)
        {
            logger.LogInformation("No elapsed booking months were available for Billing invoice seed.");
            return;
        }

        var candidateRoomIds = candidates
            .Select(candidate => candidate.Booking.RoomId)
            .Distinct()
            .ToList();

        var existingInvoices = await dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => candidateRoomIds.Contains(invoice.RoomId) &&
                              invoice.InvoiceType == InvoiceType.MonthlyUtility)
            .Select(invoice => new
            {
                invoice.RoomId,
                invoice.BillingYear,
                invoice.BillingMonth,
                invoice.ElectricityNewIndex,
                invoice.WaterNewIndex,
                invoice.Status,
                invoice.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var existingKeys = existingInvoices
            .Select(invoice => new InvoiceSeedKey(invoice.RoomId, invoice.BillingYear, invoice.BillingMonth))
            .ToHashSet();

        var continuityByRoom = existingInvoices
            .Where(invoice => invoice.Status != InvoiceStatus.Canceled)
            .GroupBy(invoice => invoice.RoomId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var latest = group
                        .OrderByDescending(invoice => invoice.BillingYear)
                        .ThenByDescending(invoice => invoice.BillingMonth)
                        .ThenByDescending(invoice => invoice.CreatedAt)
                        .First();

                    return new MeterState(
                        latest.BillingYear,
                        latest.BillingMonth,
                        latest.ElectricityNewIndex,
                        latest.WaterNewIndex);
                });

        var latestPeriodByRoom = candidates
            .GroupBy(candidate => candidate.Booking.RoomId)
            .ToDictionary(
                group => group.Key,
                group => group.Max(candidate => candidate.Year * 12 + candidate.Month));

        var random = new Random(20260531);
        var roomCache = new Dictionary<Guid, RoomBillingInfo?>();
        var contractTemplateCache = new Dictionary<ContractTemplateSeedKey, Guid?>();
        var createdCount = 0;
        var skippedCount = 0;

        foreach (var roomGroup in candidates.GroupBy(candidate => candidate.Booking.RoomId))
        {
            if (!roomCache.TryGetValue(roomGroup.Key, out var room))
            {
                room = await GetRoomBillingInfoForSeedAsync(roomBillingClient, roomGroup.Key, logger, cancellationToken);
                roomCache[roomGroup.Key] = room;
            }

            if (room is null)
            {
                skippedCount += roomGroup.Count();
                continue;
            }

            continuityByRoom.TryGetValue(roomGroup.Key, out var meterState);

            foreach (var candidate in roomGroup.OrderBy(candidate => candidate.Year).ThenBy(candidate => candidate.Month))
            {
                var candidateKey = new InvoiceSeedKey(candidate.Booking.RoomId, candidate.Year, candidate.Month);
                if (existingKeys.Contains(candidateKey))
                {
                    skippedCount++;
                    continue;
                }

                var candidatePeriod = candidate.Year * 12 + candidate.Month;
                if (meterState is not null && candidatePeriod <= meterState.Period)
                {
                    skippedCount++;
                    continue;
                }

                var billingDate = new DateOnly(candidate.Year, candidate.Month, 1);
                var createdAt = DateTime.SpecifyKind(billingDate.ToDateTime(TimeOnly.MinValue).AddDays(1), DateTimeKind.Utc);
                var electricityOldIndex = meterState?.ElectricityNewIndex ?? 0;
                var waterOldIndex = meterState?.WaterNewIndex ?? 0;
                var electricityUsage = room.Capacity * random.Next(12, 26);
                var waterUsage = room.Capacity * random.Next(2, 6);
                var electricityNewIndex = electricityOldIndex + electricityUsage;
                var waterNewIndex = waterOldIndex + waterUsage;

                InvoiceCalculationHelper.CalculationResult electricityCalculation;
                InvoiceCalculationHelper.CalculationResult waterCalculation;
                try
                {
                    var electricityTiers = await InvoiceCalculationHelper.GetApplicableTiersAsync(
                        dbContext,
                        ServiceType.Electricity,
                        room.Capacity,
                        billingDate,
                        cancellationToken);
                    var waterTiers = await InvoiceCalculationHelper.GetApplicableTiersAsync(
                        dbContext,
                        ServiceType.Water,
                        room.Capacity,
                        billingDate,
                        cancellationToken);

                    electricityCalculation = InvoiceCalculationHelper.CalculateTieredAmount(electricityUsage, electricityTiers);
                    waterCalculation = InvoiceCalculationHelper.CalculateTieredAmount(waterUsage, waterTiers);
                }
                catch (ApiException ex)
                {
                    skippedCount++;
                    logger.LogError(
                        ex,
                        "Skipping Billing invoice seed for room {RoomId}, period {Year}-{Month} because price tiers are invalid or missing.",
                        candidate.Booking.RoomId,
                        candidate.Year,
                        candidate.Month);
                    continue;
                }

                var electricitySubtotal = electricityCalculation.Amount;
                var electricityVatAmount = Math.Round(electricitySubtotal * 0.08m, 2);
                var electricityAmount = electricitySubtotal + electricityVatAmount;
                var electricitySnapshot = electricityCalculation.Snapshot.ToList();
                if (electricityVatAmount > 0)
                {
                    electricitySnapshot.Add(new InvoiceCalculationHelper.TierSnapshotItem("VAT 8%", 0, null, 0, 0.08m, electricityVatAmount));
                }

                var surcharges = CreateRandomSurcharges(random, createdAt);
                var surchargeTotal = surcharges.Sum(surcharge => surcharge.Amount);
                var isLatestForRoom = latestPeriodByRoom[candidate.Booking.RoomId] == candidatePeriod;
                var status = isLatestForRoom ? InvoiceStatus.Unpaid : InvoiceStatus.Paid;
                var paidAt = status == InvoiceStatus.Paid
                    ? DateTime.SpecifyKind(billingDate.ToDateTime(TimeOnly.MinValue).AddDays(20), DateTimeKind.Utc)
                    : (DateTime?)null;
                var updatedAt = paidAt ?? createdAt;

                var contractTemplateId = await GetContractTemplateIdForSeedAsync(
                    dbContext,
                    room.RoomTypeId,
                    billingDate,
                    contractTemplateCache,
                    cancellationToken);

                var invoice = new Invoice
                {
                    InvoiceType = InvoiceType.MonthlyUtility,
                    RoomId = candidate.Booking.RoomId,
                    BuildingCode = room.BuildingCode.Trim(),
                    Floor = room.Floor,
                    StudentId = null,
                    BillingMonth = candidate.Month,
                    BillingYear = candidate.Year,
                    ElectricityOldIndex = electricityOldIndex,
                    ElectricityNewIndex = electricityNewIndex,
                    ElectricityUsage = electricityUsage,
                    ElectricityTierSnapshot = JsonSerializer.Serialize(electricitySnapshot, JsonOptions),
                    ElectricityAmount = electricityAmount,
                    WaterOldIndex = waterOldIndex,
                    WaterNewIndex = waterNewIndex,
                    WaterUsage = waterUsage,
                    WaterTierSnapshot = JsonSerializer.Serialize(waterCalculation.Snapshot, JsonOptions),
                    WaterAmount = waterCalculation.Amount,
                    SurchargeTotal = surchargeTotal,
                    TotalAmount = electricityAmount + waterCalculation.Amount + surchargeTotal,
                    Status = status,
                    PaidAt = paidAt,
                    UpdatedByUserId = null,
                    ContractTemplateId = contractTemplateId,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt
                };

                foreach (var surcharge in surcharges)
                {
                    invoice.Surcharges.Add(surcharge);
                }

                dbContext.Invoices.Add(invoice);
                existingKeys.Add(candidateKey);
                meterState = new MeterState(candidate.Year, candidate.Month, electricityNewIndex, waterNewIndex);
                continuityByRoom[roomGroup.Key] = meterState;
                createdCount++;
            }
        }

        logger.LogInformation(
            "Billing invoice seed completed. Created {CreatedCount} invoices and skipped {SkippedCount} candidates.",
            createdCount,
            skippedCount);
    }

    private static async Task<RoomBillingInfo?> GetRoomBillingInfoForSeedAsync(
        IRoomBillingClient roomBillingClient,
        Guid roomId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var room = await roomBillingClient.GetRoomBillingInfoAsync(roomId, cancellationToken);
            if (room is null)
            {
                logger.LogWarning("Skipping Billing invoice seed for room {RoomId} because RoomService returned not found.", roomId);
            }

            return room;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Skipping Billing invoice seed for room {RoomId} because RoomService lookup failed.", roomId);
            return null;
        }
    }

    private static List<BookingBillingSyncDto> GetEligibleBookings(IReadOnlyList<BookingBillingSyncDto> bookings)
    {
        return bookings
            .Where(booking => string.Equals(booking.Status, "Confirmed", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(booking.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(booking => booking.StartDate)
            .ThenBy(booking => booking.RoomId)
            .ThenBy(booking => booking.StudentId)
            .ToList();
    }

    private static IEnumerable<(string Name, decimal Amount)> BuildBookingRegistrationSurchargeLines(BookingBillingSyncDto booking)
    {
        if (booking.BasePrice > 0)
        {
            yield return ($"Room fee {booking.TermName}".Trim(), booking.BasePrice);
        }

        foreach (var fee in (booking.Fees ?? []).Where(fee => fee.Amount > 0))
        {
            yield return (fee.FeeName.Trim(), fee.Amount);
        }
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static IReadOnlyList<(int Year, short Month)> GetCandidateMonths(
        BookingBillingSyncDto booking,
        DateOnly currentMonth)
    {
        var startMonth = new DateOnly(booking.StartDate.Year, booking.StartDate.Month, 1);
        var endMonth = new DateOnly(booking.EndDate.Year, booking.EndDate.Month, 1);
        var months = new List<(int Year, short Month)>();

        for (var month = startMonth; month <= endMonth && month <= currentMonth; month = month.AddMonths(1))
        {
            months.Add((month.Year, (short)month.Month));
        }

        return months.TakeLast(3).ToList();
    }

    private static IReadOnlyList<Surcharge> CreateRandomSurcharges(Random random, DateTime createdAt)
    {
        (string Name, int MinAmount, int MaxAmount)[] candidates =
        [
            ("Phí vệ sinh", 20000, 50000),
            ("Phí wifi", 30000, 80000),
            ("Phí bảo trì thiết bị", 20000, 120000),
            ("Phí giữ xe", 50000, 100000)
        ];

        var lineCount = random.Next(1, 4);
        return candidates
            .OrderBy(_ => random.Next())
            .Take(lineCount)
            .Select(candidate =>
            {
                var amount = random.Next(candidate.MinAmount / 1000, candidate.MaxAmount / 1000 + 1) * 1000m;
                return new Surcharge
                {
                    Name = candidate.Name,
                    Amount = amount,
                    CreatedAt = createdAt
                };
            })
            .ToList();
    }

    private static async Task<Guid?> GetContractTemplateIdForSeedAsync(
        BillingDbContext dbContext,
        Guid roomTypeId,
        DateOnly billingDate,
        Dictionary<ContractTemplateSeedKey, Guid?> cache,
        CancellationToken cancellationToken)
    {
        var cacheKey = new ContractTemplateSeedKey(roomTypeId, billingDate);
        if (cache.TryGetValue(cacheKey, out var templateId))
        {
            return templateId;
        }

        templateId = await dbContext.ContractTemplates
            .AsNoTracking()
            .Where(template => template.IsActive &&
                               template.RoomTypeId == roomTypeId &&
                               template.EffectiveFrom <= billingDate &&
                               (template.EffectiveTo == null || template.EffectiveTo >= billingDate))
            .OrderByDescending(template => template.EffectiveFrom)
            .ThenByDescending(template => template.Version)
            .Select(template => (Guid?)template.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await dbContext.ContractTemplates
                .AsNoTracking()
                .Where(template => template.IsActive &&
                                   template.RoomTypeId == null &&
                                   template.EffectiveFrom <= billingDate &&
                                   (template.EffectiveTo == null || template.EffectiveTo >= billingDate))
                .OrderByDescending(template => template.EffectiveFrom)
                .ThenByDescending(template => template.Version)
                .Select(template => (Guid?)template.Id)
                .FirstOrDefaultAsync(cancellationToken);

        cache[cacheKey] = templateId;
        return templateId;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record RoomTypeSyncDto(
        Guid Id,
        string Name,
        int Capacity,
        decimal BasePrice,
        List<string> Amenities);

    private sealed record BookingBillingSyncDto(
        Guid BookingId,
        Guid RoomId,
        Guid StudentId,
        string TermName,
        DateTime StartDate,
        DateTime EndDate,
        int NumberOfMonths,
        decimal PricePerMonth,
        decimal BasePrice,
        decimal TotalPrice,
        DateTime PaymentDueAt,
        string Status,
        DateTime CreatedAt,
        IReadOnlyList<BookingBillingSyncFeeDto> Fees);

    private sealed record BookingBillingSyncFeeDto(
        string FeeName,
        decimal Amount,
        bool IsRefundable);

    private sealed record SeedInvoiceCandidate(BookingBillingSyncDto Booking, int Year, short Month);

    private sealed record MeterState(int Year, short Month, int ElectricityNewIndex, int WaterNewIndex)
    {
        public int Period => Year * 12 + Month;
    }

    private readonly record struct TierKey(ServiceType ServiceType, int? RoomCapacity, DateOnly EffectiveFrom, decimal FromUsage);

    private readonly record struct ContractTemplateKey(string Code, int Version);

    private readonly record struct InvoiceSeedKey(Guid RoomId, int Year, short Month);

    private readonly record struct ContractTemplateSeedKey(Guid RoomTypeId, DateOnly BillingDate);
}
