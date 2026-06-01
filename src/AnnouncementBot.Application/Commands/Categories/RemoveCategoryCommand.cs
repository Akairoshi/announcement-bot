using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Categories;

public record RemoveCategoryCommand(Guid CategoryId, long ActorId = 0)
    : IRequest, IAuditableRequest
{
    long IAuditableRequest.ActorId => ActorId;
    public string ActionName => "CategoryDeleted";
    public string EntityName => "Category";
    public string? Details => null;
    public string GetEntityId() => CategoryId.ToString();
}

public class RemoveCategoryCommandHandler : IRequestHandler<RemoveCategoryCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveCategoryCommand request, CancellationToken ct)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, ct);

        if (category is null)
            throw new KeyNotFoundException($"Категория с ID {request.CategoryId} не найдена.");

        await _unitOfWork.Categories.DeleteAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
