using AnnouncementBot.Application.DTOs;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Queries;

public record GetAllAdminsQuery : IRequest<IReadOnlyList<AdminDto>>;

public class GetAllAdminsQueryHandler : IRequestHandler<GetAllAdminsQuery, IReadOnlyList<AdminDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllAdminsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AdminDto>> Handle(GetAllAdminsQuery request, CancellationToken ct)
    {
        var admins = await _unitOfWork.Users.GetAllAdminsAsync(ct);
        return admins.Select(a => new AdminDto(a.Id, a.UserName, a.Role)).ToList();
    }
}
