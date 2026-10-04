using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Products.CreateProduct;

public sealed record CreateProductCommand(CreateProductRequest Request) : ICommand<ErrorOr<ProductResponse>>;
