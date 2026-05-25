using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Application.DTOs;

namespace AnnouncementBot.Application.Queries.Templates;


public record GetAdminTemplatesQuery(long AdminId) : IRequest<IReadOnlyList<TemplateDto>>;

public class GetAdminTemplatesQueryHandler : IRequestHandler<GetAdminTemplatesQuery, IReadOnlyList<TemplateDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminTemplatesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TemplateDto>> Handle(GetAdminTemplatesQuery request, CancellationToken ct)
    {
        var templates = await _unitOfWork.Templates.GetByAdminIdAsync(request.AdminId, ct);

        return templates.Select(t => new TemplateDto(t.Id, t.Name, t.Text)).ToList();
    }
}