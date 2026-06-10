using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Application.Common.Interfaces;

namespace AnnouncementBot.Application.Commands.Users;

public record EnsureUserExistsCommand(long UserId, string? UserName)
    : IRequest<bool>, IAuditableRequest, IConditionalAudit
{
    public long ActorId => UserId;
    public string ActionName => "UserRegistered";
    public string EntityName => "User";
    public string? Details => $"UserName: {UserName}";
    public string GetEntityId() => UserId.ToString();

    public bool ShouldAudit(object? result) => result is true;
}

public class EnsureUserExistsCommandHandler : IRequestHandler<EnsureUserExistsCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public EnsureUserExistsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(EnsureUserExistsCommand request, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, ct);

        if (user is null)
        {
            var newUser = new User(request.UserId, request.UserName);
            await _unitOfWork.Users.AddAsync(newUser, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }

        if (user.UserName != request.UserName)
        {
            user.UpdateUserName(request.UserName);
            await _unitOfWork.Users.UpdateAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return false;
    }
}
