using System.Net.Http.Json;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Domain.ValueObjects;
using BookingService.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.Data;

public class SeedData
{
    private sealed record RoomSyncDto(
        Guid Id,
        string RoomName,
        decimal MonthlyPrice,
        int Capacity,
        int OccupiedCount,
        string Status);

    private sealed record StudentSyncDto(
        string Id,
        string UserName,
        string Email,
        string FullName);

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedData>>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var rooms = await SyncRoomsFromRoomServiceAsync(dbContext, httpClientFactory, logger);
        await SeedAcademicTermsAsync(dbContext);
        await SeedFeeTemplatesAsync(dbContext);
        await dbContext.SaveChangesAsync();

        await SeedActiveBookingsAsync(dbContext, httpClientFactory, rooms, logger);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<IReadOnlyList<RoomData>> SyncRoomsFromRoomServiceAsync(
        BookingDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        logger.LogInformation("Syncing Booking room projection data from RoomService...");

        try
        {
            var client = httpClientFactory.CreateClient("RoomServiceClient");
            var sourceRooms = await client.GetFromJsonAsync<List<RoomSyncDto>>("/api/rooms/sync");

            if (sourceRooms is null || sourceRooms.Count == 0)
            {
                logger.LogWarning("RoomService unavailable or returned no rooms; skipped booking occupancy seed.");
                return [];
            }

            var sourceRoomIds = sourceRooms.Select(source => source.Id).ToList();
            var existingRooms = await dbContext.Rooms
                .Where(room => sourceRoomIds.Contains(room.Id))
                .ToDictionaryAsync(room => room.Id);

            foreach (var sourceRoom in sourceRooms)
            {
                if (existingRooms.TryGetValue(sourceRoom.Id, out var room))
                {
                    room.RoomName = sourceRoom.RoomName;
                    room.MonthlyPrice = sourceRoom.MonthlyPrice;
                    room.Capacity = sourceRoom.Capacity;
                    room.OccupiedCount = sourceRoom.OccupiedCount;
                    room.Status = sourceRoom.Status;
                    continue;
                }

                dbContext.Rooms.Add(new RoomData
                {
                    Id = sourceRoom.Id,
                    RoomName = sourceRoom.RoomName,
                    MonthlyPrice = sourceRoom.MonthlyPrice,
                    Capacity = sourceRoom.Capacity,
                    OccupiedCount = sourceRoom.OccupiedCount,
                    Status = sourceRoom.Status
                });
            }

            logger.LogInformation("Synced {RoomCount} rooms from RoomService for Booking seed.", sourceRooms.Count);

            return sourceRooms
                .Select(room => new RoomData
                {
                    Id = room.Id,
                    RoomName = room.RoomName,
                    MonthlyPrice = room.MonthlyPrice,
                    Capacity = room.Capacity,
                    OccupiedCount = room.OccupiedCount,
                    Status = room.Status
                })
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RoomService unavailable; skipped booking/invoice occupancy seed.");
            return [];
        }
    }

    private static async Task SeedAcademicTermsAsync(BookingDbContext dbContext)
    {
        static DateTime CreateUtcDate(int year, int month, int day)
        {
            return DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);
        }

        var seedTerms = new List<AcademicTermData>
        {
            new()
            {
                TermName = "HK1_2025_2026",
                StartDate = CreateUtcDate(2025, 9, 1),
                EndDate = CreateUtcDate(2026, 1, 31),
                NumberOfMonths = 5
            },
            new()
            {
                TermName = "HK2_2025_2026",
                StartDate = CreateUtcDate(2026, 2, 1),
                EndDate = CreateUtcDate(2026, 6, 30),
                NumberOfMonths = 5
            },
            new()
            {
                TermName = "HKH_2025_2026",
                StartDate = CreateUtcDate(2026, 7, 1),
                EndDate = CreateUtcDate(2026, 8, 31),
                NumberOfMonths = 2
            },
            new()
            {
                TermName = "HK1_2026_2027",
                StartDate = CreateUtcDate(2026, 9, 1),
                EndDate = CreateUtcDate(2027, 1, 31),
                NumberOfMonths = 5
            },
            new()
            {
                TermName = "HK2_2026_2027",
                StartDate = CreateUtcDate(2027, 2, 1),
                EndDate = CreateUtcDate(2027, 6, 30),
                NumberOfMonths = 5
            }
        };

        var existingTermNames = await dbContext.AcademicTerms
            .Select(term => term.TermName)
            .ToListAsync();

        var missingTerms = seedTerms
            .Where(term => !existingTermNames.Contains(term.TermName))
            .ToList();

        if (missingTerms.Count > 0)
        {
            await dbContext.AcademicTerms.AddRangeAsync(missingTerms);
        }
    }

    private static async Task SeedFeeTemplatesAsync(BookingDbContext dbContext)
    {
        var seedFees = new List<FeeTemplate>
        {
            new()
            {
                FeeCode = "HO_SO",
                FeeName = "Ho so ky tuc xa",
                Amount = 60000,
                IsMandatory = true,
                Description = "One-time dormitory registration paperwork fee.",
                IsRefundable = false
            },
            new()
            {
                FeeCode = "THE_CHAN",
                FeeName = "The chan tai san",
                Amount = 100000,
                IsMandatory = true,
                Description = "Refundable asset deposit.",
                IsRefundable = true
            },
            new()
            {
                FeeCode = "BHTN_12T",
                FeeName = "Bao hiem tai nan 12 thang",
                Amount = 30000,
                IsMandatory = true,
                Description = "Mandatory accident insurance.",
                IsRefundable = false
            },
            new()
            {
                FeeCode = "BHYT_12T",
                FeeName = "Bao hiem y te 12 thang",
                Amount = 680400,
                IsMandatory = false,
                Description = "Optional health insurance collection.",
                IsRefundable = false
            },
            new()
            {
                FeeCode = "BHYT_15T",
                FeeName = "Bao hiem y te 15 thang",
                Amount = 850500,
                IsMandatory = false,
                Description = "Optional freshman health insurance collection.",
                IsRefundable = false
            }
        };

        var existingFeeCodes = await dbContext.FeeTemplates
            .Select(fee => fee.FeeCode)
            .ToListAsync();

        var missingFees = seedFees
            .Where(fee => !existingFeeCodes.Contains(fee.FeeCode))
            .ToList();

        if (missingFees.Count > 0)
        {
            await dbContext.FeeTemplates.AddRangeAsync(missingFees);
        }
    }

    private static async Task SeedActiveBookingsAsync(
        BookingDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IReadOnlyList<RoomData> syncedRooms,
        ILogger logger)
    {
        var occupiedRooms = syncedRooms
            .Where(room => room.OccupiedCount > 0)
            .OrderBy(room => room.RoomName)
            .ThenBy(room => room.Id)
            .ToList();

        if (occupiedRooms.Count == 0)
        {
            logger.LogInformation("No occupied rooms were available for Booking seed.");
            return;
        }

        var termData = await SelectSeedTermAsync(dbContext);
        if (termData is null)
        {
            logger.LogWarning("No academic term was available for Booking seed.");
            return;
        }

        var term = new AcademicTerm(
            termData.TermName,
            termData.StartDate,
            termData.EndDate,
            termData.NumberOfMonths);

        var students = await GetStudentUsersFromIdentityAsync(httpClientFactory, logger);
        if (students.Count == 0)
        {
            logger.LogWarning("IdentityService returned no student users; skipped Booking seed.");
            return;
        }

        var existingBookings = await dbContext.Bookings
            .Include(booking => booking.Fees)
            .Where(booking => booking.Term.TermName == term.TermName &&
                              (booking.Status == BookingStatus.Active ||
                               booking.Status == BookingStatus.Confirmed))
            .ToListAsync();

        var assignedStudentIds = existingBookings
            .Select(booking => booking.UserId)
            .ToHashSet();

        var availableStudents = students
            .Select(student => Guid.TryParse(student.Id, out var userId) ? userId : (Guid?)null)
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .Where(userId => !assignedStudentIds.Contains(userId))
            .ToList();

        var studentCursor = 0;
        var createdCount = 0;
        const int logBatchSize = 200;
        var mandatoryFees = await dbContext.FeeTemplates
            .AsNoTracking()
            .Where(fee => fee.IsMandatory)
            .OrderBy(fee => fee.FeeCode)
            .Select(fee => new SeedBookingFee(fee.FeeName, fee.Amount, fee.IsRefundable))
            .ToListAsync();

        foreach (var room in occupiedRooms)
        {
            var existingRoomCount = existingBookings.Count(booking => booking.RoomId == room.Id);
            var missingCount = Math.Max(room.OccupiedCount - existingRoomCount, 0);

            for (var index = 0; index < missingCount; index++)
            {
                if (studentCursor >= availableStudents.Count)
                {
                    logger.LogWarning(
                        "Only seeded {SeededCount}/{OccupiedCount} bookings for room {RoomId}; not enough student users.",
                        existingRoomCount + index,
                        room.OccupiedCount,
                        room.Id);
                    break;
                }

                var studentId = availableStudents[studentCursor++];
                var createdAt = CreateDeterministicCreatedAt(term.StartDate, room.Id, studentId, index);
                await InsertSeedBookingAsync(
                    dbContext,
                    room.Id,
                    studentId,
                    term,
                    room.MonthlyPrice,
                    createdAt,
                    mandatoryFees);
                createdCount++;

                if (createdCount % logBatchSize == 0)
                {
                    logger.LogInformation(
                        "Booking seed saved {CreatedCount} active bookings so far for term {TermName}.",
                        createdCount,
                        term.TermName);
                }
            }
        }

        logger.LogInformation(
            "Booking seed completed for term {TermName}. Created {CreatedCount} active bookings.",
            term.TermName,
            createdCount);
    }

    private static async Task InsertSeedBookingAsync(
        BookingDbContext dbContext,
        Guid roomId,
        Guid studentId,
        AcademicTerm term,
        decimal pricePerMonth,
        DateTime createdAt,
        IReadOnlyList<SeedBookingFee> fees)
    {
        var bookingId = Guid.NewGuid();
        var paymentDueAt = createdAt.AddHours(48);
        var basePrice = pricePerMonth * term.NumberOfMonths;
        var feeTotal = fees.Where(fee => fee.Amount > 0).Sum(fee => fee.Amount);
        var totalPrice = basePrice + feeTotal;
        var activeStatus = (int)BookingStatus.Active;

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Bookings"
                ("Id", "RoomId", "UserId", "TermName", "StartDate", "EndDate", "NumberOfMonths",
                 "PricePerMonth", "BasePrice", "TotalPrice", "Status", "CreatedAt", "UpdatedAt", "PaymentDueAt")
            VALUES
                ({bookingId}, {roomId}, {studentId}, {term.TermName}, {term.StartDate}, {term.EndDate}, {term.NumberOfMonths},
                 {pricePerMonth}, {basePrice}, {totalPrice}, {activeStatus}, {createdAt}, {createdAt}, {paymentDueAt});
            """);

        foreach (var fee in fees.Where(fee => fee.Amount > 0))
        {
            var feeId = Guid.NewGuid();
            var feeName = fee.FeeName.Trim();

            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "BookingFees"
                    ("Id", "FeeName", "Amount", "IsRefundable", "BookingId")
                VALUES
                    ({feeId}, {feeName}, {fee.Amount}, {fee.IsRefundable}, {bookingId});
                """);
        }

        await transaction.CommitAsync();
    }

    private static async Task<AcademicTermData?> SelectSeedTermAsync(BookingDbContext dbContext)
    {
        const string seedTermName = "HK2_2025_2026";
        var now = DateTime.UtcNow.Date;
        var terms = await dbContext.AcademicTerms
            .AsNoTracking()
            .OrderBy(term => term.StartDate)
            .ToListAsync();

        return terms.FirstOrDefault(term => term.TermName == seedTermName)
               ?? terms.FirstOrDefault(term => term.StartDate.Date <= now && term.EndDate.Date >= now)
               ?? terms.Where(term => term.StartDate.Date <= now)
                   .OrderByDescending(term => term.StartDate)
                   .FirstOrDefault()
               ?? terms.FirstOrDefault();
    }

    private static async Task<IReadOnlyList<StudentSyncDto>> GetStudentUsersFromIdentityAsync(
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        try
        {
            var client = httpClientFactory.CreateClient("IdentityServiceClient");
            var students = await client.GetFromJsonAsync<List<StudentSyncDto>>("/api/auth/users/students/sync");
            return students ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "IdentityService unavailable; skipped Booking student seed.");
            return [];
        }
    }

    private static DateTime CreateDeterministicCreatedAt(
        DateTime termStartDate,
        Guid roomId,
        Guid studentId,
        int roomIndex)
    {
        var seed = HashCode.Combine(roomId, studentId, roomIndex);
        var dayOffset = Math.Abs(seed % 7);
        var minuteOffset = Math.Abs(seed % 480);
        return DateTime.SpecifyKind(termStartDate.Date.AddDays(dayOffset).AddHours(8).AddMinutes(minuteOffset), DateTimeKind.Utc);
    }
}
