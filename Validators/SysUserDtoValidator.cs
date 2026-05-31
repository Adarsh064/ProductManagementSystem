using FluentValidation;
using ProductManagementSystem.DTOs;

namespace ProductManagementSystem.Validators
{
    public class SysUserDtoValidator : AbstractValidator<SysUserDto>
    {
        public SysUserDtoValidator()
        {
            RuleFor(x => x.LoginId)
                 .NotEmpty().WithMessage("User Name is required.")
                 .EmailAddress().WithMessage("Invalid User Name format.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.");

            RuleFor(x => x.UserType).NotEmpty().WithMessage("User Type is required.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        }

    }
}
