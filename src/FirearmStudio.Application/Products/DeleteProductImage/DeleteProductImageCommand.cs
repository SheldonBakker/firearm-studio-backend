using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Products.DeleteProductImage;

public sealed record DeleteProductImageCommand(Guid Id) : ICommand<ErrorOr<Deleted>>;
