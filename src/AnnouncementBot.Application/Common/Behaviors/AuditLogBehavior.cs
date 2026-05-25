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

        if (request is IAuditableRequest auditableRequest)
        {
            var log = new AuditLog(
                auditableRequest.ActorId,
                auditableRequest.ActionName,
                auditableRequest.EntityName,
                auditableRequest.GetEntityId(),
                auditableRequest.Details
            );

            await _unitOfWork.AuditLogs.AddAsync(log, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return response;
    }
}