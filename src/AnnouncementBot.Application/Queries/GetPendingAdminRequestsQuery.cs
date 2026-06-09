using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Application.DTOs;

namespace AnnouncementBot.Application.Queries.AdminRequests;

// Добавляем параметр Limit в рекорд. По умолчанию будет 30, если не передать другое
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
        // Передаем request.Limit в репозиторий, чтобы обрезать пачку на уровне БД
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