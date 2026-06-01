using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Templates;

public record UpdateTemplateCommand(Guid TemplateId, string? NewName, string? NewText, long ActorId = 0)
    : IRequest, IAuditableRequest
{
    long IAuditableRequest.ActorId => ActorId;
    public string ActionName => "TemplateUpdated";
    public string EntityName => "Template";
    public string? Details => $"NewName: {NewName}, NewText: {NewText}";
    public string GetEntityId() => TemplateId.ToString();
}

public class UpdateTemplateCommandHandler : IRequestHandler<UpdateTemplateCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTemplateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateTemplateCommand request, CancellationToken ct)
    {
        var template = await _unitOfWork.Templates.GetByIdAsync(request.TemplateId, ct);

        if (template is null)
            throw new KeyNotFoundException($"Шаблон с ID {request.TemplateId} не найден.");

        if (!string.IsNullOrWhiteSpace(request.NewName))
            template.UpdateName(request.NewName);

        if (!string.IsNullOrWhiteSpace(request.NewText))
            template.UpdateText(request.NewText);

        await _unitOfWork.Templates.UpdateAsync(template, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
