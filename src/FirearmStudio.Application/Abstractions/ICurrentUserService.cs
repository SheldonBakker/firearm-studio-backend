using FirearmStudio.Domain.Authentication;

namespace FirearmStudio.Application.Abstractions;

public interface ICurrentUserService
{
    CurrentUser User { get; }
}
