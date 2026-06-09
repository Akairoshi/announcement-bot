using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Application.DTOs;

namespace AnnouncementBot.Application.Queries;

public record GetAdminCategoriesQuery(long AdminId) : IRequest<IReadOnlyList<AdminCategoryDto>>;

public class GetAdminCategoriesQueryHandler : IRequestHandler<GetAdminCategoriesQuery, IReadOnlyList<AdminCategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminCategoriesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AdminCategoryDto>> Handle(GetAdminCategoriesQuery request, CancellationToken ct)
    {
        var accesses = await _unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(request.AdminId, ct);

        var result = new List<AdminCategoryDto>();

        foreach (var access in accesses)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(access.CategoryId, ct);
            if (category is not null)
            {
                result.Add(new AdminCategoryDto(category.Id, category.Name));
            }
        }

        return result;
    }
}