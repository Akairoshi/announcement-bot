using AnnouncementBot.Application.DTOs;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Queries;

public record GetAnnouncementsQuery(long UserId, UserRole Role) : IRequest<IReadOnlyList<AnnouncementDto>>;

public class GetAnnouncementsQueryHandler : IRequestHandler<GetAnnouncementsQuery, IReadOnlyList<AnnouncementDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAnnouncementsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AnnouncementDto>> Handle(GetAnnouncementsQuery request, CancellationToken ct)
    {
        var announcements = request.Role switch
        {
            UserRole.SuperAdmin => await _unitOfWork.Announcements.GetAllAsync(ct),
            UserRole.Admin => await _unitOfWork.Announcements.GetByAdminIdAsync(request.UserId, ct),
            _ => await GetUserAnnouncementsAsync(request.UserId, ct)
        };

        var result = new List<AnnouncementDto>();

        foreach (var a in announcements)
        {
            var categoryName = "Удалённая категория";
            if (a.CategoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(a.CategoryId.Value, ct);
                if (category is not null)
                    categoryName = category.Name;
            }

            result.Add(new AnnouncementDto(a.Id, a.Text, categoryName, a.CreatedAt));
        }

        return result.OrderByDescending(a => a.CreatedAt).ToList();
    }

    private async Task<IReadOnlyList<Domain.Entities.Announcement>> GetUserAnnouncementsAsync(long userId, CancellationToken ct)
    {
        var subscriptions = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId, ct);
        var result = new List<Domain.Entities.Announcement>();

        foreach (var sub in subscriptions)
        {
            var list = await _unitOfWork.Announcements.GetByCategoryIdAsync(sub.CategoryId, ct);
            result.AddRange(list);
        }

        return result;
    }
}
