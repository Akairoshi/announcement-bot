using FluentValidation;
using AnnouncementBot.Application.Commands.Users;

namespace AnnouncementBot.Application.Validators;

public class EnsureExistUserCommandValidator : AbstractValidator<EnsureUserExistsCommand>
{
    public EnsureExistUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("ID пользователя должен быть больше нуля.");

        When(x => x.UserName != null, () =>
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Имя пользователя не может быть пустым.")
                .MaximumLength(200).WithMessage("Имя пользователя слишком длинное.");
        });
    }
}