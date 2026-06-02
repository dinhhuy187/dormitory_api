using Microsoft.EntityFrameworkCore;

namespace Profile.API.Infrastructure.Database;

public sealed class ProfileSeedWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ProfileSeedWorker> logger) : BackgroundService
{
    private const int MaxAttempts = 30;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ProfileDbContext>();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var result = await SeedData.SeedAsync(dbContext, httpClientFactory, logger, stoppingToken);

                if (result.CreatedCount > 0 || result.UpdatedCount > 0)
                {
                    return;
                }

                if (await dbContext.UserProfiles.AnyAsync(stoppingToken))
                {
                    logger.LogInformation("Profile seed found existing profile data and no updates were required.");
                    return;
                }

                logger.LogWarning(
                    "Profile seed attempt {Attempt}/{MaxAttempts} did not receive student data. Retrying in {RetryDelaySeconds}s.",
                    attempt,
                    MaxAttempts,
                    RetryDelay.TotalSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Profile seed attempt {Attempt}/{MaxAttempts} failed. Retrying in {RetryDelaySeconds}s.",
                    attempt,
                    MaxAttempts,
                    RetryDelay.TotalSeconds);
            }

            if (attempt < MaxAttempts)
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }

        logger.LogError("IdentityService unavailable or returned no student data. Profile seed skipped after {MaxAttempts} attempts.", MaxAttempts);
    }
}
