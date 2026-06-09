using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Application.DTOs;

namespace AnnouncementBot.Application.Queries;


public record GetAdminTemplateQuery(long AdminId) : IRequest<IReadOnlyList<TemplateDto>>;

public class GetAdminTemplateQueryHandler : IRequestHandler<GetAdminTemplateQuery, IReadOnlyList<TemplateDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminTemplateQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TemplateDto>> Handle(GetAdminTemplateQuery request, CancellationToken ct)
    {
        var templates = await _unitOfWork.Templates.GetByAdminIdAsync(request.AdminId, ct);

        return templates.Select(t => new TemplateDto(t.Id, t.Name, t.Text)).ToList();
    }
}