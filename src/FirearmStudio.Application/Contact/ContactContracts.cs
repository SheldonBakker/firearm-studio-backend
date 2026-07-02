namespace FirearmStudio.Application.Contact;

public sealed record ContactFormRequest(string FullName, string Email, string? Company, string Message);
