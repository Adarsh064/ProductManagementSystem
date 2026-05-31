using FluentValidation;
using ProductManagementSystem.DTOs;

namespace ProductManagementSystem.Validators
{
    public class ItemDtoValidator : AbstractValidator<ItemDto>
    {
        public ItemDtoValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be a non-negative value.");

            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("A valid ProductId is required.");
        }
    }
    }
