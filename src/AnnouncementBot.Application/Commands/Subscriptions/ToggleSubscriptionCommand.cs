using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Subscriptions;

public record ToggleSubscriptionCommand(long UserId, Guid CategoryId) : IRequest;

public class ToggleSubscriptionCommandHandler : IRequestHandler<ToggleSubscriptionCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public ToggleSubscriptionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ToggleSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await _unitOfWork.Subscriptions
            .GetByUserAndCategoryAsync(request.UserId, request.CategoryId, ct);

        if (subscription is not null)
        {
            await _unitOfWork.Subscriptions.DeleteAsync(subscription, ct);
        }
        else
        {
            var newSubscription = new Subscription(request.UserId, request.CategoryId);
            await _unitOfWork.Subscriptions.AddAsync(newSubscription, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}