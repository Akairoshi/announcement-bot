using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Application.Commands.Users;

public record EnsureUserExistsCommand(long UserId, string? UserName) : IRequest;

public class EnsureUserExistsCommandHandler : IRequestHandler<EnsureUserExistsCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EnsureUserExistsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EnsureUserExistsCommand request, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, ct);

        if (user is null)
        {
            var newUser = new User(request.UserId, request.UserName);
            await _unitOfWork.Users.AddAsync(newUser, ct);
        }
        else if (user.UserName != request.UserName)
        {
            user.UpdateUserName(request.UserName);
            await _unitOfWork.Users.UpdateAsync(user, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}