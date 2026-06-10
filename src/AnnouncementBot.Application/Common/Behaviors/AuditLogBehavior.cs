using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Application.Common.Interfaces;

namespace AnnouncementBot.Application.Common.Behaviors;

public class AuditLogBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        if (request is not IAuditableRequest auditableRequest)
            return response;

        if (request is IConditionalAudit conditional && !conditional.ShouldAudit(response))
            return response;

        var entityId = response is Guid guid
            ? guid.ToString()
            : auditableRequest.GetEntityId();

        var actionName = response is bool subscribed && auditableRequest.ActionName == "SubscriptionToggled"
            ? (subscribed ? "CategorySubscribed" : "CategoryUnsubscribed")
            : auditableRequest.ActionName;

        var log = new AuditLog(
            auditableRequest.ActorId,
            actionName,
            auditableRequest.EntityName,
            entityId,
            auditableRequest.Details);

        await _unitOfWork.AuditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return response;
    }
}
