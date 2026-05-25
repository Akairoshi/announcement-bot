using FluentValidation;
using AnnouncementBot.Application.Commands.Categories;

namespace AnnouncementBot.Application.Validators;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Не указан ID категории для обновления.");

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("Новое название категории не может быть пустым.")
            .MaximumLength(100).WithMessage("Название категории слишком длинное (макс. 100 символов).");
    }
}