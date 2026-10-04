using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Products.UpdateProduct;

public sealed record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : ICommand<ErrorOr<ProductResponse>>;
