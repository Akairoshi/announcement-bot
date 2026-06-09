using MediatR;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Application.DTOs;
using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Application.Queries;

public record GetDeliveriesWithErrorCodeQuery(DeliveryErrorStatus ErrorStatus) : IRequest<IReadOnlyList<DeliveryDto>>;

public class GetDeliveriesWithErrorCodeQueryHandler : IRequestHandler<GetDeliveriesWithErrorCodeQuery, IReadOnlyList<DeliveryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDeliveriesWithErrorCodeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<DeliveryDto>> Handle(GetDeliveriesWithErrorCodeQuery request, CancellationToken ct)
    {
        var deliveries = await _unitOfWork.DeliveryStatuses.GetWithErrorCodeAsync((int)request.ErrorStatus);

        return deliveries
            .Select(d => new DeliveryDto(
                d.Id,
                d.AnnouncementId,
                d.UserId,
                (int)d.ErrorStatus,
                d.LastAttemptAt))
            .ToList();
    }
}
