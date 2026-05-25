using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Announcements;

public record CreateAnnouncementCommand(
    string Text,
    Guid CategoryId,
    long CreatedById,
    Guid? TemplateId = null) : IRequest<Guid>;

public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAnnouncementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAnnouncementCommand request, CancellationToken ct)
    {
        var announcement = new Announcement(
            request.Text,
            request.CategoryId,
            request.CreatedById,
            request.TemplateId);

        await _unitOfWork.Announcements.AddAsync(announcement, ct);

        var subscribers = await _unitOfWork.Subscriptions
            .GetByCategoryIdAsync(request.CategoryId, ct);

        if (subscribers.Any())
        {
            var deliveryStatuses = subscribers
                .Select(s => new DeliveryStatus(announcement.Id, s.UserId))
                .ToList();

            await _unitOfWork.DeliveryStatuses.AddRangeAsync(deliveryStatuses, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return announcement.Id;
    }
}