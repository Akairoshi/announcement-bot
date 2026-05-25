using FluentValidation;
using AnnouncementBot.Application.Commands.AdminRequests;
using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Application.Validators;

public class CreateAdminRequestCommandValidator : AbstractValidator<CreateAdminRequestCommand>
{
    public CreateAdminRequestCommandValidator()
    {
        RuleFor(x => x.RequesterId)
            .GreaterThan(0).WithMessage("Некорректный Telegram ID заявителя.");

        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("Необходимо указать причину подачи заявки.")
            .MaximumLength(500).WithMessage("Описание причины слишком длинное (макс. 500 символов).");

        // Если это переназначение, то TargetId (кому отдаем права) обязателен
        When(x => x.RequestType == AdminRequestType.Reassignment, () =>
        {
            RuleFor(x => x.TargetId)
                .NotNull().WithMessage("Для переназначения прав необходимо указать целевого пользователя.")
                .GreaterThan(0).WithMessage("Некорректный Telegram ID целевого пользователя.");
        });
    }
}