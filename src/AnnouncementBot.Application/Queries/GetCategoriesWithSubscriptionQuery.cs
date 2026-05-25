using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Application.DTOs;

namespace AnnouncementBot.Application.Queries.Categories;

public record GetCategoriesWithSubscriptionQuery(long UserId) : IRequest<IReadOnlyList<CategorySubscriptionDto>>;

public class GetCategoriesWithSubscriptionQueryHandler : IRequestHandler<GetCategoriesWithSubscriptionQuery, IReadOnlyList<CategorySubscriptionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoriesWithSubscriptionQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CategorySubscriptionDto>> Handle(GetCategoriesWithSubscriptionQuery request, CancellationToken ct)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(ct);

        var userSubscriptions = await _unitOfWork.Subscriptions.GetByUserIdAsync(request.UserId, ct);
        var subscribedCategoryIds = userSubscriptions.Select(s => s.CategoryId).ToHashSet();

        return categories.Select(c => new CategorySubscriptionDto(
            c.Id,
            c.Name,
            subscribedCategoryIds.Contains(c.Id)
        )).ToList();
    }
}