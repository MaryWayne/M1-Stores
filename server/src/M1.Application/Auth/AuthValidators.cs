using FluentValidation;

namespace M1.Application.Auth;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2).MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[a-zA-Z]").WithMessage("Password must contain a letter.");
    }
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[a-zA-Z]").WithMessage("Password must contain a letter.");
    }
}

public class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName).MinimumLength(2).MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.FullName));
        RuleFor(x => x.NewPassword).MinimumLength(8)
            .Matches("[0-9]").Matches("[a-zA-Z]")
            .When(x => !string.IsNullOrEmpty(x.NewPassword));
    }
}
