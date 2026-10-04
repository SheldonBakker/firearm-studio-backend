using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Products.UploadProductImage;

public sealed record UploadProductImageCommand(Guid Id, Stream Content, string ContentType, long Length)
    : ICommand<ErrorOr<ProductResponse>>;
