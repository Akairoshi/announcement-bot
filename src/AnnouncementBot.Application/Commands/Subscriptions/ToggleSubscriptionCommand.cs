using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Application.Commands.Users
{
    public record ToggleSubscriptionCommand(long UserId, Guid SubscriptionId) : IRequest;
    public class ToggleSubscriptionCommandHandler : IRequestHandler<ToggleSubscriptionCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        public ToggleSubscriptionCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(ToggleSubscriptionCommand request, CancellationToken ct)
        {

        }
    }
}
