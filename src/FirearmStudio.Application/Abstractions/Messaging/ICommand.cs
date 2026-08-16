using MediatR;

namespace FirearmStudio.Application.Abstractions.Messaging;

public interface ICommand<TResponse> : IRequest<TResponse>;
