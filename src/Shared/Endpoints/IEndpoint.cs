using Microsoft.AspNetCore.Routing;

namespace Shared.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder endpoint);
}
