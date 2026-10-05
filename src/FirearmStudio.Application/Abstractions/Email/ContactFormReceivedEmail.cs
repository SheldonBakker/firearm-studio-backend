namespace FirearmStudio.Application.Abstractions.Email;

public sealed record ContactFormReceivedEmail(
    string SubmitterName, string SubmitterEmail, string? SubmitterCompany, string Message) : EmailMessage;
