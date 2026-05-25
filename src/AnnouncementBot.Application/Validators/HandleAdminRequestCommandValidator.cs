using FluentValidation;
using AnnouncementBot.Application.Commands.AdminRequests;

namespace AnnouncementBot.Application.Validators;

public class HandleAdminRequestCommandValidator : AbstractValidator<HandleAdminRequestCommand>
{
    public HandleAdminRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Не указан ID обрабатываемой заявки.");

        RuleFor(x => x.SuperAdminId)
            .GreaterThan(0).WithMessage("Некорректный Telegram ID Супер-Админа.");

        When(x => x.IsApproved && x.CategoryName != null, () =>
        {
            RuleFor(x => x.CategoryName)
                .NotEmpty().WithMessage("Название категории не может быть пустым.")
                .MaximumLength(100).WithMessage("Название категории слишком длинное.");
        });
    }
}