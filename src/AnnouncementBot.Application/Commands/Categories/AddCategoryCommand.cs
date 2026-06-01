using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Categories;

public record AddCategoryCommand(string Name, long CreatedById)
    : IRequest<Guid>, IAuditableRequest
{
    public long ActorId => CreatedById;
    public string ActionName => "CategoryCreated";
    public string EntityName => "Category";
    public string? Details => $"Name: {Name}";
    public string GetEntityId() => string.Empty;
}

public class AddCategoryCommandHandler : IRequestHandler<AddCategoryCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddCategoryCommand request, CancellationToken ct)
    {
        var exists = await _unitOfWork.Categories.ExistsAsync(request.Name, ct);

        if (exists)
            throw new InvalidOperationException($"Категория '{request.Name}' уже существует.");

        var category = new Category(request.Name, request.CreatedById);

        await _unitOfWork.Categories.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return category.Id;
    }
}
