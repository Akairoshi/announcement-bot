using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Categories;

public record UpdateCategoryCommand(Guid CategoryId, string NewName, long ActorId = 0)
    : IRequest, IAuditableRequest
{
    long IAuditableRequest.ActorId => ActorId;
    public string ActionName => "CategoryUpdated";
    public string EntityName => "Category";
    public string? Details => $"NewName: {NewName}";
    public string GetEntityId() => CategoryId.ToString();
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NewName))
            throw new ArgumentException("Название категории не может быть пустым.", nameof(request.NewName));

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, ct);

        if (category is null)
            throw new KeyNotFoundException($"Категория с ID {request.CategoryId} не найдена.");

        var nameExists = await _unitOfWork.Categories.ExistsAsync(request.NewName, ct);

        if (nameExists)
            throw new InvalidOperationException($"Категория '{request.NewName}' уже существует.");

        category.UpdateName(request.NewName);

        await _unitOfWork.Categories.UpdateAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
