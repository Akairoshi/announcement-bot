using FluentValidation;
using AnnouncementBot.Application.Commands.Subscriptions;

namespace AnnouncementBot.Application.Validators;

public class ToggleSubscriptionCommandValidator : AbstractValidator<ToggleSubscriptionCommand>
{
    public ToggleSubscriptionCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Некорректный Telegram ID пользователя.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Идентификатор категории (Guid) не может быть пустым.");
    }
}