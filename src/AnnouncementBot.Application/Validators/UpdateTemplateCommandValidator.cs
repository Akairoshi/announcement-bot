using FluentValidation;
using AnnouncementBot.Application.Commands.Templates;

namespace AnnouncementBot.Application.Validators;

public class UpdateTemplateCommandValidator : AbstractValidator<UpdateTemplateCommand>
{
    public UpdateTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("Не указан ID шаблона для обновления.");

        When(x => x.NewName != null, () =>
        {
            RuleFor(x => x.NewName)
                .NotEmpty().WithMessage("Новое имя шаблона не может быть пустым.")
                .MaximumLength(150).WithMessage("Новое имя шаблона слишком длинное.");
        });

        When(x => x.NewText != null, () =>
        {
            RuleFor(x => x.NewText)
                .NotEmpty().WithMessage("Новый текст шаблона не может быть пустым.")
                .MaximumLength(4000).WithMessage("Новый текст шаблона слишком длинный.");
        });
    }
}