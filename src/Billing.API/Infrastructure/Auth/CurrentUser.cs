using System.Security.Claims;

namespace Billing.API.Infrastructure.Auth;

public static class CurrentUser
{
    public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var rawUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("studentId")
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("userId");

        return Guid.TryParse(rawUserId, out userId);
    }
}
