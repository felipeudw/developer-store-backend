using FluentValidation;
using Sales.Api.Features.Sales;

namespace Sales.Api.Validation
{
    public sealed class SaleItemRequestValidator : AbstractValidator<SaleItemRequest>
    {
        public SaleItemRequestValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Item.ProductId is required")
                .MaximumLength(64);

            RuleFor(x => x.ProductName)
                .NotEmpty().WithMessage("Item.ProductName is required")
                .MaximumLength(256);

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Item.Quantity must be greater than zero")
                .LessThanOrEqualTo(20).WithMessage("It's not possible to sell above 20 identical items");

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0).WithMessage("Item.UnitPrice must be greater than zero");
        }
    }

    public sealed class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
    {
        public CreateSaleRequestValidator()
        {
            RuleFor(x => x.SaleNumber)
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.CustomerName)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(x => x.BranchId)
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.BranchName)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(x => x.Items)
                .NotNull()
                .NotEmpty();

            RuleForEach(x => x.Items)
                .SetValidator(new SaleItemRequestValidator());
        }
    }

    public sealed class UpdateSaleRequestValidator : AbstractValidator<UpdateSaleRequest>
    {
        public UpdateSaleRequestValidator()
        {
            When(x => x.SaleNumber is not null, () =>
            {
                RuleFor(x => x.SaleNumber!)
                    .NotEmpty()
                    .MaximumLength(64);
            });

            When(x => x.CustomerId is not null, () =>
            {
                RuleFor(x => x.CustomerId!)
                    .NotEmpty()
                    .MaximumLength(64);
            });

            When(x => x.CustomerName is not null, () =>
            {
                RuleFor(x => x.CustomerName!)
                    .NotEmpty()
                    .MaximumLength(256);
            });

            When(x => x.BranchId is not null, () =>
            {
                RuleFor(x => x.BranchId!)
                    .NotEmpty()
                    .MaximumLength(64);
            });

            When(x => x.BranchName is not null, () =>
            {
                RuleFor(x => x.BranchName!)
                    .NotEmpty()
                    .MaximumLength(256);
            });

            // Keep parity with existing behavior: Items must not be null (may be empty)
            RuleFor(x => x.Items)
                .NotNull();

            RuleForEach(x => x.Items)
                .SetValidator(new SaleItemRequestValidator());
        }
    }
}