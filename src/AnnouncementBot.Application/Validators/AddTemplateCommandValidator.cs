using FluentValidation;
using AnnouncementBot.Application.Commands.Templates;

namespace AnnouncementBot.Application.Validators;

public class AddTemplateCommandValidator : AbstractValidator<AddTemplateCommand>
{
    public AddTemplateCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название шаблона не может быть пустым.")
            .MaximumLength(150).WithMessage("Название шаблона слишком длинное (макс. 150 символов).");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Текст шаблона не может быть пустым.")
            .MaximumLength(4000).WithMessage("Текст шаблона превышает лимит Telegram (макс. 4000 символов).");

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("Некорректный Telegram ID создателя.");
    }
}