using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using PlatformPlatform.AccountManagement.Core.TelemetryEvents;
using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.AccountManagement.Core.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.TelemetryEvents;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Core.Users.Commands;

public sealed record CreateUserCommand(string TenantId, string Email, UserRole UserRole, bool EmailConfirmed)
    : ICommand, IRequest<Result<UserId>>
{
    public TenantId GetTenantId()
    {
        return new TenantId(TenantId);
    }
}

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IUserRepository userRepository, ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());

        RuleFor(x => x.TenantId)
            .MustAsync((x, cancellationToken) => tenantRepository.ExistsAsync(new TenantId(x), cancellationToken))
            .WithMessage(x => $"The tenant '{x.TenantId}' does not exist.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x)
            .MustAsync((x, cancellationToken)
                => userRepository.IsEmailFreeAsync(x.GetTenantId(), x.Email, cancellationToken)
            )
            .WithName("Email")
            .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public sealed class CreateUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<CreateUserCommand, Result<UserId>>
{
    private static readonly HttpClient Client = new();

    public async Task<Result<UserId>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var gravatarUrl = await GetGravatarProfileUrlIfExists(command.Email);

        var user = User.Create(command.GetTenantId(), command.Email, command.UserRole, command.EmailConfirmed, gravatarUrl);

        await userRepository.AddAsync(user, cancellationToken);

        events.CollectEvent(new UserCreated(command.GetTenantId(), gravatarUrl is not null));

        return user.Id;
    }

    private async Task<string?> GetGravatarProfileUrlIfExists(string email)
    {
        var hash = Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(email)));
        var gravatarUrl = $"https://gravatar.com/avatar/{hash.ToLowerInvariant()}";
        // The d=404 instructs Gravatar to return 404 the email has no Gravatar account
        var httpResponseMessage = await Client.GetAsync($"{gravatarUrl}?d=404");
        return httpResponseMessage.StatusCode == HttpStatusCode.OK ? gravatarUrl : null;
    }
}