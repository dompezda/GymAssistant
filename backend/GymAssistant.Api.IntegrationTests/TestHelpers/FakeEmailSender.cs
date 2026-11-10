using System.Collections.Concurrent;

public class FakeEmailSender : IEmailSender
{
    public ConcurrentQueue<(string Email, string Link)> SentEmails { get; } = new();

    public Task SendPasswordResetEmailAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        SentEmails.Enqueue((email, resetLink));
        return Task.CompletedTask;
    }
}