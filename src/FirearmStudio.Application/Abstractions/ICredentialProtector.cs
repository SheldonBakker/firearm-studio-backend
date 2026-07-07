namespace FirearmStudio.Application.Abstractions;

public interface ICredentialProtector
{
    string Protect(string value);

    string Unprotect(string protectedValue);
}
