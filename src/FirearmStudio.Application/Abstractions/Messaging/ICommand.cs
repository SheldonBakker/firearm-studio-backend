using MediatR;

namespace FirearmStudio.Application.Abstractions.Messaging;

public interface ICommand : IRequest;

public interface ICommand<TResponse> : IRequest<TResponse>;
