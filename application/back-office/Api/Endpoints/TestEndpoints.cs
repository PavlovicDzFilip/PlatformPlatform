using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.BackOffice.Api.Endpoints;

public sealed class TestEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office/test";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Test").RequireAuthorization();

        group.MapPost("/login/test", async Task<ApiResult<StartLoginResponse>> (StartLoginCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartLoginResponse>().AllowAnonymous();
    }
}

[PublicAPI]
public sealed record StartLoginCommand(string Email) : ICommand, IRequest<Result<StartLoginResponse>>;

[PublicAPI]
public sealed record StartLoginResponse(string LoginId, int ValidForSeconds, int myvalue = 3);
