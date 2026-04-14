using System.Net.Http.Headers;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DeanOfficeCourseWork.Services;

public class NotificationService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public NotificationService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task SendEmailCodeAsync(string email, string code, string subject)
    {
        var host = _configuration["Notifications:Smtp:Host"]?.Trim();
        var portText = _configuration["Notifications:Smtp:Port"]?.Trim();
        var username = _configuration["Notifications:Smtp:Username"]?.Trim();
        var password = _configuration["Notifications:Smtp:Password"];
        var from = _configuration["Notifications:Smtp:From"]?.Trim();
        var enableSsl = bool.TryParse(_configuration["Notifications:Smtp:EnableSsl"], out var ssl) && ssl;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portText) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("SMTP не настроен. Заполните Notifications:Smtp в appsettings.json или user secrets.");
        }

        password = password.Replace(" ", string.Empty).Trim();
        if (!int.TryParse(portText, out var port))
        {
            throw new InvalidOperationException("SMTP порт указан неверно. Для Gmail используйте 587.");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = $"Ваш код подтверждения: {code}. Код действует 10 минут."
        };

        var socketOptions = enableSsl
            ? port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(host, port, socketOptions);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"SMTP ошибка при отправке через {host}:{port}, SSL={enableSsl}, From={from}. Для Gmail нужен порт 587 и пароль приложения. Детали: {ex.Message}",
                ex);
        }
    }

    public async Task<string> SendSmsCodeAsync(string phoneNumber, string code)
    {
        var provider = _configuration["Notifications:Sms:Provider"];
        if (!string.Equals(provider, "Twilio", StringComparison.OrdinalIgnoreCase))
        {
            return $"Тестовое SMS для {VerificationCodeService.MaskDestination(phoneNumber)}. Код: {code}";
        }

        var accountSid = _configuration["Notifications:Twilio:AccountSid"];
        var authToken = _configuration["Notifications:Twilio:AuthToken"];
        var from = _configuration["Notifications:Twilio:From"];

        if (string.IsNullOrWhiteSpace(accountSid) ||
            string.IsNullOrWhiteSpace(authToken) ||
            string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("Twilio SMS API не настроен. Заполните Notifications:Twilio в appsettings.json или user secrets.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json");
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["From"] = from,
            ["To"] = phoneNumber,
            ["Body"] = $"Ваш код подтверждения: {code}. Код действует 10 минут."
        });

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Twilio вернул ошибку {(int)response.StatusCode}: {body}");
        }

        return $"Код отправлен через Twilio на {VerificationCodeService.MaskDestination(phoneNumber)}.";
    }
}
