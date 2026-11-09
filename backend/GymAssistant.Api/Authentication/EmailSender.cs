using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class EmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public EmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendPasswordResetEmailAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        var msg = new MimeMessage();

        msg.From.Add(new MailboxAddress(
            _options.FromName,
            _options.FromEmail));

        msg.To.Add(MailboxAddress.Parse(email));

        msg.Subject = "Resetowanie hasła - GymAssistant";

        msg.Body = new TextPart("plain")
        {
            Text = $"Aby zresetować hasło, otwórz link: {resetLink}"
        };
        using var client = new SmtpClient();
        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            SecureSocketOptions.None,
            cancellationToken);

        await client.SendAsync(msg,cancellationToken);
        await client.DisconnectAsync(true,cancellationToken);
    }
}