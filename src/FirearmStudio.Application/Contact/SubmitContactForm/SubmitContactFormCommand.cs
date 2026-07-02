using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Contact.SubmitContactForm;

public sealed record SubmitContactFormCommand(ContactFormRequest Request) : ICommand<ErrorOr<Success>>;
