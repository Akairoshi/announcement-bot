using FluentValidation;
using AnnouncementBot.Application.Commands.Users;

namespace AnnouncementBot.Application.Validators;

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Некорректный Telegram ID пользователя.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Указана несуществующая роль пользователя.");
    }
}