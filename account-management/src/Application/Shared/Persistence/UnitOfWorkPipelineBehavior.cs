using MediatR;
using PlatformPlatform.AccountManagement.Domain.Shared;

namespace PlatformPlatform.AccountManagement.Application.Shared.Persistence;

/// <summary>
///     The UnitOfWorkPipelineBehavior class is a MediatR pipeline behavior that encapsulates the unit of work pattern.
///     It is called after the handling of a Command, and ensures that any changes are committed to the database only
///     after the command is successfully handled. If an exception occurs the UnitOfWork.Commit will never be called.
/// </summary>
public sealed class UnitOfWorkPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkPipelineBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        await _unitOfWork.CommitAsync(cancellationToken);

        return response;
    }
}