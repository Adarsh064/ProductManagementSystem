using FluentValidation;
using ProductManagementSystem.DTOs;

namespace ProductManagementSystem.Validators
{
    public class ProductDtoValidator : AbstractValidator<ProductDto>
    {
        public ProductDtoValidator()
        {
            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(255).WithMessage("Product name must not exceed 255 characters.");

            RuleForEach(x => x.Items).SetValidator(new ItemDtoValidator());
        }
    }
    }
