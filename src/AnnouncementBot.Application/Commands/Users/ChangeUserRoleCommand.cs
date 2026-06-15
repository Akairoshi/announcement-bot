using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Users;

public record ChangeUserRoleCommand(long UserId, UserRole Role, long ActorId = 0)
    : IRequest<Unit>, IAuditableRequest
{
    long IAuditableRequest.ActorId => ActorId;
    public string ActionName => Role == UserRole.Admin ? "AdminAppointed" : "AdminRemoved";
    public string EntityName => "User";
    public string? Details => $"NewRole: {Role}";
    public string GetEntityId() => UserId.ToString();
}

public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserRoleCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, ct);

        if (user is null)
            throw new KeyNotFoundException($"Пользователь {request.UserId} не найден.");

        user.ChangeRole(request.Role);

        await _unitOfWork.Users.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
