using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : ICommand<ErrorOr<Deleted>>;
