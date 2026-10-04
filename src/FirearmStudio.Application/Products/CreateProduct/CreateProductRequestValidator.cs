using FluentValidation;

namespace FirearmStudio.Application.Products.CreateProduct;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(4000)
            .When(request => request.Description is not null);

        RuleFor(request => request.Sku)
            .MaximumLength(64)
            .Must(sku => sku is null || sku.Trim().Length > 0)
            .WithMessage("SKU must not be whitespace.")
            .When(request => request.Sku is not null);

        RuleFor(request => request.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.CostPrice)
            .GreaterThanOrEqualTo(0)
            .When(request => request.CostPrice.HasValue);

        RuleFor(request => request.Category)
            .MaximumLength(100)
            .When(request => request.Category is not null);

        RuleFor(request => request.StockQuantity)
            .GreaterThanOrEqualTo(0);
    }
}
