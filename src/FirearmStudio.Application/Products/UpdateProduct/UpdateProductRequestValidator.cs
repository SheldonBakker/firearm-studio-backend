using FirearmStudio.Application.Model;
using FluentValidation;

namespace FirearmStudio.Application.Products.UpdateProduct;

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => OptionalHelpers.HasAtLeastOneSet(request))
            .WithMessage("At least one field must be supplied.");

        RuleFor(request => request.Name.Value)
            .NotEmpty()
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateProductRequest.Name))
            .When(request => request.Name.IsSet);

        RuleFor(request => request.Description.Value)
            .MaximumLength(4000)
            .OverridePropertyName(nameof(UpdateProductRequest.Description))
            .When(request => request.Description.IsSet && request.Description.Value is not null);

        RuleFor(request => request.Sku.Value)
            .MaximumLength(64)
            .Must(sku => sku is null || sku.Trim().Length > 0)
            .WithMessage("SKU must not be whitespace.")
            .OverridePropertyName(nameof(UpdateProductRequest.Sku))
            .When(request => request.Sku.IsSet && request.Sku.Value is not null);

        RuleFor(request => request.Price.Value)
            .GreaterThanOrEqualTo(0)
            .OverridePropertyName(nameof(UpdateProductRequest.Price))
            .When(request => request.Price.IsSet);

        RuleFor(request => request.CostPrice.Value)
            .GreaterThanOrEqualTo(0)
            .OverridePropertyName(nameof(UpdateProductRequest.CostPrice))
            .When(request => request.CostPrice.IsSet && request.CostPrice.Value is not null);

        RuleFor(request => request.Category.Value)
            .MaximumLength(100)
            .OverridePropertyName(nameof(UpdateProductRequest.Category))
            .When(request => request.Category.IsSet && request.Category.Value is not null);

        RuleFor(request => request.StockQuantity.Value)
            .GreaterThanOrEqualTo(0)
            .OverridePropertyName(nameof(UpdateProductRequest.StockQuantity))
            .When(request => request.StockQuantity.IsSet);
    }
}
