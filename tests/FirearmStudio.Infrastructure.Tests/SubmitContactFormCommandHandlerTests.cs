using System.Collections.Concurrent;
using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using FirearmStudio.Application.Contact;
using FirearmStudio.Application.Contact.SubmitContactForm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public sealed class SubmitContactFormCommandHandlerTests
{
    private sealed class FakeContactDirectory : IContactDirectory
    {
        public ContactEntry? LastEntry { get; private set; }
        public bool ShouldThrow { get; init; }

        public Task AddContactAsync(ContactEntry entry, CancellationToken cancellationToken)
        {
            if (ShouldThrow)
            {
                throw new InvalidOperationException("directory failure");
            }

            LastEntry = entry;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailSender : ITransactionalEmailSender
    {
        private readonly ConcurrentBag<EmailMessage> _sent = [];
        public bool ShouldThrow { get; init; }
        public ConcurrentBag<EmailMessage> Sent => _sent;

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (ShouldThrow)
            {
                throw new InvalidOperationException("sender failure");
            }

            _sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private static SubmitContactFormCommandHandler Build(
        FakeContactDirectory? directory = null,
        FakeEmailSender? sender = null)
    {
        return new SubmitContactFormCommandHandler(
            directory ?? new FakeContactDirectory(),
            sender ?? new FakeEmailSender(),
            NullLogger<SubmitContactFormCommandHandler>.Instance);
    }

    [Theory]
    [InlineData(" John  Smith ", "John", "Smith")]
    [InlineData("Cher", "Cher", null)]
    [InlineData("First Last", "First", "Last")]
    [InlineData("First Middle Last", "First", "Middle Last")]
    public async Task Name_is_split_on_first_whitespace_run(string fullName, string expectedFirst, string? expectedLast)
    {
        var directory = new FakeContactDirectory();
        var handler = Build(directory: directory);

        await handler.Handle(
            new SubmitContactFormCommand(new ContactFormRequest(fullName, "user@example.com", null, "Hello")),
            default);

        Assert.NotNull(directory.LastEntry);
        Assert.Equal(expectedFirst, directory.LastEntry!.FirstName);
        Assert.Equal(expectedLast, directory.LastEntry.LastName);
    }

    [Fact]
    public async Task Both_emails_are_sent_with_correct_records()
    {
        var sender = new FakeEmailSender();
        var handler = Build(sender: sender);
        var request = new ContactFormRequest("Jane Doe", "jane@example.com", "ACME", "Hello there");

        await handler.Handle(new SubmitContactFormCommand(request), default);

        var received = Assert.Single(sender.Sent.OfType<ContactFormReceivedEmail>());
        Assert.Equal("Jane Doe", received.SubmitterName);
        Assert.Equal("jane@example.com", received.SubmitterEmail);
        Assert.Equal("ACME", received.SubmitterCompany);
        Assert.Equal("Hello there", received.Message);

        var ack = Assert.Single(sender.Sent.OfType<ContactFormAcknowledgementEmail>());
        Assert.Equal("jane@example.com", ack.RecipientEmail);
        Assert.Equal("Jane Doe", ack.RecipientName);
    }

    [Fact]
    public async Task Throwing_directory_still_yields_Success_and_both_emails_are_sent()
    {
        var directory = new FakeContactDirectory { ShouldThrow = true };
        var sender = new FakeEmailSender();
        var handler = Build(directory: directory, sender: sender);

        var result = await handler.Handle(
            new SubmitContactFormCommand(
                new ContactFormRequest("John Smith", "john@example.com", null, "Message")),
            default);

        Assert.False(result.IsError);
        Assert.Equal(2, sender.Sent.Count);
    }

    [Fact]
    public async Task Throwing_sender_still_yields_Success()
    {
        var directory = new FakeContactDirectory();
        var sender = new FakeEmailSender { ShouldThrow = true };
        var handler = Build(directory: directory, sender: sender);

        var result = await handler.Handle(
            new SubmitContactFormCommand(
                new ContactFormRequest("John Smith", "john@example.com", null, "Message")),
            default);

        Assert.False(result.IsError);
        Assert.NotNull(directory.LastEntry);
    }
}
