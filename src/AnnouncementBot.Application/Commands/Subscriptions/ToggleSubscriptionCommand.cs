using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Subscriptions;

public record ToggleSubscriptionCommand(long UserId, Guid CategoryId)
    : IRequest<bool>, IAuditableRequest
{
    public long ActorId => UserId;
    public string ActionName => "SubscriptionToggled";
    public string EntityName => "Subscription";
    public string? Details => $"CategoryId: {CategoryId}";
    public string GetEntityId() => CategoryId.ToString();
}

public class ToggleSubscriptionCommandHandler : IRequestHandler<ToggleSubscriptionCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ToggleSubscriptionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ToggleSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await _unitOfWork.Subscriptions
            .GetByUserAndCategoryAsync(request.UserId, request.CategoryId, ct);

        if (subscription is not null)
        {
            await _unitOfWork.Subscriptions.DeleteAsync(subscription, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return false;
        }

        var newSubscription = new Subscription(request.UserId, request.CategoryId);
        await _unitOfWork.Subscriptions.AddAsync(newSubscription, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
