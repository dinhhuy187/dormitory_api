using System.Net.Http.Json;

namespace Chat.API.Infrastructure.Services;

public class ProfileService(HttpClient httpClient) : IProfileService
{
    public async Task<Dictionary<string, UserProfileDto>> GetProfilesAsync(
        IEnumerable<string> userIds,
        string accessToken,
        CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        try
        {
            // Đính kèm token vào header
            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post, "api/profile/batch");

            requestMessage.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            requestMessage.Content = JsonContent.Create(new { UserIds = ids });

            var response = await httpClient.SendAsync(requestMessage, ct);

            if (!response.IsSuccessStatusCode)
                return [];

            var result = await response.Content
                .ReadFromJsonAsync<List<UserProfileDto>>(ct);

            return result?.ToDictionary(p => p.UserId) ?? [];
        }
        catch
        {
            return [];
        }
    }
}