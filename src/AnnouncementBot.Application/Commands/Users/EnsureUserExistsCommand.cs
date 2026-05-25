using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Application.Commands.Users
{
    public record EnsureExistUserCommand(long UserId, string? UserName) : IRequest;
    public class EnsureUserExistsCommandHandler : IRequestHandler<EnsureExistUserCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        public EnsureUserExistsCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(EnsureExistUserCommand request, CancellationToken ct)
        {
            var exist = await _unitOfWork.Users.ExistsAsync(request.UserId, ct);
            
            if (exist) return;

            var user = new User(request.UserId, request.UserName);
            await _unitOfWork.Users.AddAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
