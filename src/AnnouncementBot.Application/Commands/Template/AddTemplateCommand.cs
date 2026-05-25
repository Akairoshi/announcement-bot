using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.Templates;

public record AddTemplateCommand(string Name, string Text, long CreatedById) : IRequest<Guid>;

public class AddTemplateCommandHandler : IRequestHandler<AddTemplateCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;

    public AddTemplateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddTemplateCommand request, CancellationToken ct)
    {
        var template = new Template(request.Name, request.Text, request.CreatedById);

        await _unitOfWork.Templates.AddAsync(template, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return template.Id;
    }
}