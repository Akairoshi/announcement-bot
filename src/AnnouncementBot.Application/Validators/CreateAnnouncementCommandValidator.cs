using FluentValidation;
using AnnouncementBot.Application.Commands.Announcements;

namespace AnnouncementBot.Application.Validators;

public class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Текст объявления не может быть пустым.")
            .MaximumLength(4000).WithMessage("Текст объявления слишком длинный для Telegram (макс. 4000 символов).");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Необходимо указать ID категории.");

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("Некорректный Telegram ID создателя.");
    }
}