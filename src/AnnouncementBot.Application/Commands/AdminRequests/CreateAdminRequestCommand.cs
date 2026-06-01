using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;
using MediatR;

namespace AnnouncementBot.Application.Commands.AdminRequests;

public record CreateAdminRequestCommand(
    long RequesterId,
    AdminRequestType RequestType,
    string Reason,
    long? TargetId = null) : IRequest<Guid>, IAuditableRequest
{
    public long ActorId => RequesterId;
    public string ActionName => RequestType == AdminRequestType.Assignment
        ? "AdminRequestCreated"
        : "AdminReassignmentRequestCreated";
    public string EntityName => "AdminRequest";
    public string? Details => $"Type: {RequestType}, Reason: {Reason}, TargetId: {TargetId}";
    public string GetEntityId() => string.Empty;
}

public class CreateAdminRequestCommandHandler : IRequestHandler<CreateAdminRequestCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAdminRequestCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAdminRequestCommand request, CancellationToken ct)
    {
        if (request.RequestType == AdminRequestType.Reassignment)
        {
            if (request.TargetId is null)
                throw new ArgumentException("Для переназначения необходимо указать целевого пользователя.");

            var targetUserExists = await _unitOfWork.Users.ExistsAsync(request.TargetId.Value, ct);
            if (!targetUserExists)
                throw new KeyNotFoundException($"Целевой пользователь с ID {request.TargetId} не найден.");
        }

        var adminRequest = new AdminRequest(
            request.RequesterId,
            request.TargetId,
            request.RequestType,
            request.Reason);

        await _unitOfWork.AdminRequests.AddAsync(adminRequest, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return adminRequest.Id;
    }
}
