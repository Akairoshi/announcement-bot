using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;
using MediatR;

namespace AnnouncementBot.Application.Commands.AdminRequests;

public record CreateAdminRequestCommand(
    long RequesterId,
    AdminRequestType RequestType,
    string Details,
    long? TargetId = null) : IRequest<Guid>;

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

        // Конструктор: (long requesterId, long? targetId, AdminRequestType type, string details)
        var adminRequest = new AdminRequest(
            request.RequesterId,
            request.TargetId,
            request.RequestType,
            request.Details);

        await _unitOfWork.AdminRequests.AddAsync(adminRequest, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return adminRequest.Id;
    }
}