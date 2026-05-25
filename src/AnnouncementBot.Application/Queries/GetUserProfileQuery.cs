using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Application.DTOs;

namespace AnnouncementBot.Application.Queries.Users;

public record GetUserProfileQuery(long UserId) : IRequest<UserProfileDto>;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, ct);
        if (user is null)
            throw new KeyNotFoundException($"Пользователь с ID {request.UserId} не найден.");

        var categoryNames = new List<string>();

        if (user.Role == UserRole.Admin)
        {
            var accesses = await _unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(request.UserId, ct);
            foreach (var access in accesses)
            {
                var cat = await _unitOfWork.Categories.GetByIdAsync(access.CategoryId, ct);
                if (cat is not null) categoryNames.Add(cat.Name);
            }
        }
        else
        {
            var subs = await _unitOfWork.Subscriptions.GetByUserIdAsync(request.UserId, ct);
            foreach (var sub in subs)
            {
                var cat = await _unitOfWork.Categories.GetByIdAsync(sub.CategoryId, ct);
                if (cat is not null) categoryNames.Add(cat.Name);
            }
        }

        return new UserProfileDto(user.Id, user.UserName, user.Role, categoryNames);
    }
}