using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Products.GetProduct;

public sealed record GetProductQuery(Guid Id) : IQuery<ErrorOr<ProductResponse>>;
