using FirearmStudio.Application.Abstractions.Email;

namespace FirearmStudio.Application.Abstractions;

public interface IContactDirectory
{
    Task AddContactAsync(ContactEntry entry, CancellationToken cancellationToken);
}
