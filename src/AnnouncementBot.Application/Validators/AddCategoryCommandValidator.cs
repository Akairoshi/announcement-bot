using FluentValidation;
using AnnouncementBot.Application.Commands.Categories;

namespace AnnouncementBot.Application.Validators;

public class AddCategoryCommandValidator : AbstractValidator<AddCategoryCommand>
{
    public AddCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название категории не может быть пустым.")
            .MaximumLength(100).WithMessage("Название категории слишком длинное (макс. 100 символов).");

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("Некорректный Telegram ID создателя.");
    }
}