using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Announcements;

// Команда принимает текст, категорию и ID админа. TemplateId делаем опциональным
public record CreateAnnouncementCommand(string Text, Guid CategoryId, long CreatedById, Guid? TemplateId = null)
    : IRequest<Guid>, IAuditableRequest
{
    public long ActorId => CreatedById;
    public string ActionName => "AnnouncementCreated";
    public string EntityName => "Announcement";
    public string? Details => $"CategoryId: {CategoryId}, TemplateId: {TemplateId}";
    public string GetEntityId() => string.Empty; // ID определится после сохранения
}

public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAnnouncementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateAnnouncementCommand request, CancellationToken ct)
    {
        // 1. Создаем и сохраняем само объявление
        var announcement = new Announcement(
            request.Text,
            request.CategoryId,
            request.CreatedById,
            request.TemplateId
        );

        await _unitOfWork.Announcements.AddAsync(announcement, ct);

        // 2. Получаем список всех подписчиков на эту категорию
        // Предполагается, что у репозитория подписок есть метод получения по CategoryId
        var subscriptions = await _unitOfWork.Subscriptions.GetByCategoryIdAsync(request.CategoryId, ct);

        if (subscriptions.Any())
        {
            // 3. Формируем список статусов доставки в состоянии Pending
            var deliveryStatuses = subscriptions.Select(sub =>
                new DeliveryStatus(announcement.Id, sub.UserId)
            ).ToList();

            // 4. Добавляем их пачкой в репозиторий статусов доставки
            // Если у твоего репозитория нет метода AddRangeAsync, можно использовать обычный цикл AddAsync,
            // но AddRangeAsync для коллекций гораздо эффективнее
            await _unitOfWork.DeliveryStatuses.AddRangeAsync(deliveryStatuses, ct);
        }

        // 5. Коммитим всю транзакцию целиком (и объявление, и все статусы)
        await _unitOfWork.SaveChangesAsync(ct);

        return announcement.Id;
    }
}