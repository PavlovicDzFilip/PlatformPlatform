using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Services;
using PlatformPlatform.SharedKernel.SinglePageApp;
using PlatformPlatform.SharedKernel.TelemetryEvents;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Users.Commands;

[PublicAPI]
public sealed record InviteUserCommand(string Email) : ICommand, IRequest<Result>;

public sealed class InviteUserValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());

        RuleFor(x => x)
            .MustAsync((x, cancellationToken) => userRepository.IsEmailFreeAsync(x.Email, cancellationToken))
            .WithName("Email")
            .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public sealed class InviteUserHandler(
    IEmailService emailService,
    IExecutionContext executionContext,
    IMediator mediator,
    ITelemetryEventsCollector events
) : IRequestHandler<InviteUserCommand, Result>
{
    public async Task<Result> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.UserRole != UserRole.Owner.ToString())
        {
            return Result.Forbidden("Only owners are allowed to invite other users.");
        }

        await mediator.Send(new CreateUserCommand(executionContext.TenantId!, command.Email, UserRole.Member, false), cancellationToken);

        events.CollectEvent(new UserInvited());

        var loginPath = $"{Environment.GetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey)}/login";
        var inviter = $"{executionContext.UserInfo.FirstName} {executionContext.UserInfo.LastName}".Trim();
        inviter = inviter.Length > 0 ? inviter : executionContext.UserInfo.Email;
        await emailService.SendAsync(command.Email.ToLower(), $"You have been invited to join {executionContext.TenantId} on PlatformPlatform",
            $"""
             <h1 style="text-align:center;font-family:sans-serif;font-size:20px">
               <b>{inviter}</b> invited you to join <b>{executionContext.TenantId}</b> on PlatformPlatform.
             </h1>
             <p style="text-align:center;font-family:sans-serif;font-size:16px">
               To gain access, <a href="{loginPath}" target="blank">go to this page in your open browser</a> and login using <b>{command.Email.ToLower()}</b>.
             </p>
             """,
            cancellationToken
        );

        return Result.Success();
    }
}
