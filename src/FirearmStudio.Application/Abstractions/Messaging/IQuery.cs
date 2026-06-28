using MediatR;

namespace FirearmStudio.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<TResponse>;
