using AnnouncementBot.Application.DTOs;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Queries;

public record GetAllCategoriesQuery : IRequest<IReadOnlyList<AdminCategoryDto>>;

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, IReadOnlyList<AdminCategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllCategoriesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AdminCategoryDto>> Handle(GetAllCategoriesQuery request, CancellationToken ct)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(ct);
        return categories.Select(c => new AdminCategoryDto(c.Id, c.Name)).ToList();
    }
}