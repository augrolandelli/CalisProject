using CalisApi.Dtos;
using FluentValidation;

namespace CalisApi.Dtos.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PhotoUrl).MaximumLength(1024);
    }
}

public class CreateAchievementRequestValidator : AbstractValidator<CreateAchievementRequest>
{
    public CreateAchievementRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Icon).NotEmpty().MaximumLength(50);
    }
}
