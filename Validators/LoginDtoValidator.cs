using FluentValidation;
using ProductManagementSystem.DTOs;

namespace ProductManagementSystem.Validators
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.UserName).NotEmpty().NotNull().WithMessage("User Name is required.")
                .EmailAddress().WithMessage("Invalid User Name format.");

            RuleFor(x => x.Password).NotEmpty().NotNull().WithMessage("Password is required.");
        }
    }
}
