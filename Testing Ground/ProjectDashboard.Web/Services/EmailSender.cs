using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace ProjectDashboard.Web.Services;

public class EmailSender
{
    private readonly IConfiguration _cfg;
    public EmailSender(IConfiguration cfg) { _cfg = cfg; }

    public async Task SendAsync(string to, string subject, string body)
    {
        var host = _cfg["Smtp:Host"] ?? "localhost";
        var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 25;
        var useSsl = bool.TryParse(_cfg["Smtp:UseSsl"], out var s) && s;
        var user = _cfg["Smtp:User"];
        var pass = _cfg["Smtp:Password"];
        var from = _cfg["Smtp:From"] ?? "noreply@example.local";

        using var client = new SmtpClient(host, port) { EnableSsl = useSsl };
        if (!string.IsNullOrEmpty(user)) client.Credentials = new NetworkCredential(user, pass);
        var msg = new MailMessage(from, to, subject, body) { IsBodyHtml = false };
        await client.SendMailAsync(msg);
    }
}
