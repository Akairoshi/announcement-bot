using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Application.DTOs;

namespace AnnouncementBot.Application.Queries;

public record GetPendingAdminRequestsQuery(int Limit = 30) : IRequest<IReadOnlyList<PendingRequestDto>>;

public class GetPendingAdminRequestsQueryHandler : IRequestHandler<GetPendingAdminRequestsQuery, IReadOnlyList<PendingRequestDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPendingAdminRequestsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PendingRequestDto>> Handle(GetPendingAdminRequestsQuery request, CancellationToken ct)
    {
        var pendingRequests = await _unitOfWork.AdminRequests.GetPendingAsync(request.Limit, ct);

        return pendingRequests.Select(r => new PendingRequestDto(
            r.Id,
            r.RequesterId,
            r.Type,
            r.Details,
            r.TargetId,
            r.CreatedAt
        )).ToList();
    }
}