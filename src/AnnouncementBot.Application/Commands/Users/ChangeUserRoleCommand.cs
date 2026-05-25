using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Application.Commands.Users
{
    public record ChangeUserRoleCommand(long UserId, UserRole Role) : IRequest;
    public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        public ChangeUserRoleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(ChangeUserRoleCommand request, CancellationToken ct)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, ct);

            if (user == null)
                throw new InvalidOperationException($"User {request.UserId} not found.");

            user.ChangeRole(request.Role);

            await _unitOfWork.Users.UpdateAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
