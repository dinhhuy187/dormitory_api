using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared;

namespace Billing.API.Infrastructure.Services;

public sealed class BookingContractClient(
    IHttpClientFactory httpClientFactory,
    ILogger<BookingContractClient> logger) : IBookingContractClient
{
    public async Task<IReadOnlyList<BookingContractInfo>> GetMyBookingsAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return [];
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient("BookingServiceClient");
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/bookings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "BookingService returned {StatusCode} while loading contract booking enrichment.",
                    (int)response.StatusCode);
                return [];
            }

            var apiResponse = await response.Content
                .ReadFromJsonAsync<ApiResponse<List<BookingContractInfo>>>(cancellationToken);

            return apiResponse?.Data ?? [];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BookingService booking enrichment request failed.");
            return [];
        }
    }
}
