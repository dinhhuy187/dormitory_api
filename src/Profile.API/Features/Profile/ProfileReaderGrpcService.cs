using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Profile.API.Infrastructure.Database;
using Shared.Grpc.Profile;

namespace Profile.API.Features.Profile;

public sealed class ProfileReaderGrpcService(ProfileDbContext dbContext) : ProfileReader.ProfileReaderBase
{
    public override async Task<GetAvatarByUserIdResponse> GetAvatarByUserId(
        GetAvatarByUserIdRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "user_id is required."));
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == request.UserId)
            .Select(profile => new
            {
                profile.UserId,
                profile.AvatarUrl
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        if (profile is null)
        {
            return new GetAvatarByUserIdResponse
            {
                UserId = request.UserId,
                AvatarUrl = string.Empty,
                ProfileExists = false
            };
        }

        return new GetAvatarByUserIdResponse
        {
            UserId = profile.UserId,
            AvatarUrl = profile.AvatarUrl ?? string.Empty,
            ProfileExists = true
        };
    }
}
