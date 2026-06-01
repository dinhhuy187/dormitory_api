using BookingService.Domain.Enums;
using BookingService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Booking;

namespace BookingService.Infrastructure.HostedServices;

public sealed class PendingBookingExpirationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PendingBookingExpirationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(1);
    private const string ExpirationReason = "Payment timeout after 48 hours.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(ScanInterval);

        await PublishExpiredBookingsAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PublishExpiredBookingsAsync(stoppingToken);
        }
    }

    private async Task PublishExpiredBookingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var now = DateTime.UtcNow;

            var pendingBookings = await dbContext.Bookings
                .AsNoTracking()
                .Where(b => b.Status == BookingStatus.Pending)
                .Select(b => new { b.Id, b.PaymentDueAt })
                .ToListAsync(cancellationToken);

            Console.WriteLine($"[PendingBookingExpirationWorker] Current UTC: {now:yyyy-MM-dd HH:mm:ss.fff}. Total Pending bookings: {pendingBookings.Count}");
            foreach (var b in pendingBookings)
            {
                Console.WriteLine($"[PendingBookingExpirationWorker] Tracked Pending Booking ID: {b.Id}, PaymentDueAt (from DB): {b.PaymentDueAt:yyyy-MM-dd HH:mm:ss.fff} (Kind: {b.PaymentDueAt.Kind})");
            }

            var expiredBookings = pendingBookings
                .Where(booking => booking.PaymentDueAt <= now)
                .ToList();

            Console.WriteLine($"[PendingBookingExpirationWorker] Found {expiredBookings.Count} expired bookings.");

            foreach (var booking in expiredBookings)
            {
                Console.WriteLine($"[PendingBookingExpirationWorker] Publishing expiration for Booking: {booking.Id}, PaymentDueAt (UTC): {booking.PaymentDueAt:yyyy-MM-dd HH:mm:ss.fff}");
                await publishEndpoint.Publish(new BookingPaymentExpiredEvent(
                    booking.Id,
                    now,
                    ExpirationReason), cancellationToken);
            }

            if (expiredBookings.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                Console.WriteLine($"[PendingBookingExpirationWorker] Successfully saved {expiredBookings.Count} expiration messages to outbox.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish expired booking events.");
        }
    }
}
