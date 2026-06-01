using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Templates;

public record RemoveTemplateCommand(Guid TemplateId, long ActorId = 0)
    : IRequest, IAuditableRequest
{
    long IAuditableRequest.ActorId => ActorId;
    public string ActionName => "TemplateDeleted";
    public string EntityName => "Template";
    public string? Details => null;
    public string GetEntityId() => TemplateId.ToString();
}

public class RemoveTemplateCommandHandler : IRequestHandler<RemoveTemplateCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public RemoveTemplateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveTemplateCommand request, CancellationToken ct)
    {
        var template = await _unitOfWork.Templates.GetByIdAsync(request.TemplateId, ct);

        if (template is null)
            throw new KeyNotFoundException($"Шаблон с ID {request.TemplateId} не найден.");

        await _unitOfWork.Templates.DeleteAsync(template, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
