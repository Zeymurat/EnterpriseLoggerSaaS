using System.Net;
using System.Net.Mail;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;
using Microsoft.Extensions.Logging;

namespace EnterpriseLogger.Infrastructure.Email;

public class SmtpEmailNotificationService : IEmailNotificationService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailNotificationService> _logger;

    public SmtpEmailNotificationService(
        EmailSettings settings,
        ILogger<SmtpEmailNotificationService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public bool IsEnabled => _settings.Enabled;

    public Task SendPaymentDueAsync(
        int tenantId,
        string recipientEmail,
        string tenantName,
        string packageName,
        DateTime? paymentDueBy,
        CancellationToken cancellationToken = default)
    {
        var dueText = paymentDueBy?.ToString("dd.MM.yyyy") ?? "en kısa sürede";
        var body =
            $"Merhaba,\n\n{tenantName} hesabınız için {packageName} paketi yenilendi. " +
            $"Havale/EFT ödemenizi {dueText} tarihine kadar tamamlayın.\n\nEnterpriseLogger";

        return SendAsync(
            recipientEmail,
            "Ödeme bekleniyor — EnterpriseLogger",
            body,
            cancellationToken);
    }

    public Task SendGraceReminderAsync(
        int tenantId,
        string recipientEmail,
        string tenantName,
        string packageName,
        DateTime graceEndsAt,
        CancellationToken cancellationToken = default)
    {
        var body =
            $"Merhaba,\n\n{tenantName} hesabınızda {packageName} aboneliği için ödeme süresi " +
            $"{graceEndsAt:dd.MM.yyyy} tarihinde sona erecek. Ödeme yapılmazsa Free pakete düşersiniz.\n\nEnterpriseLogger";

        return SendAsync(
            recipientEmail,
            "Ödeme hatırlatması — EnterpriseLogger",
            body,
            cancellationToken);
    }

    public Task SendQuotaWarningAsync(
        int tenantId,
        string recipientEmail,
        string tenantName,
        string packageName,
        int monthlyLogCount,
        int monthlyLimit,
        CancellationToken cancellationToken = default)
    {
        var percent = monthlyLimit > 0 ? (int)Math.Round(monthlyLogCount * 100.0 / monthlyLimit) : 0;
        var body =
            $"Merhaba,\n\n{tenantName} hesabınızda {packageName} paketinin aylık log kotasının " +
            $"%{percent}'ine ulaştınız ({monthlyLogCount:N0} / {monthlyLimit:N0}).\n\nEnterpriseLogger";

        return SendAsync(
            recipientEmail,
            "Kota uyarısı — EnterpriseLogger",
            body,
            cancellationToken);
    }

    private async Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(recipientEmail))
            return;

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = string.IsNullOrWhiteSpace(_settings.Username)
                    ? null
                    : new NetworkCredential(_settings.Username, _settings.Password),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };
            message.To.Add(recipientEmail);

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "E-posta gönderilemedi: {Recipient}", recipientEmail);
        }
    }
}
