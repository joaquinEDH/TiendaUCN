using Resend;
using Serilog;
using Tienda.src.Application.Services.Interfaces;

namespace Tienda.src.Application.Services.Implements
{
    /// <summary>
    /// Servicio para enviar correos (Resend).
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public EmailService(IResend resend, IConfiguration configuration, IWebHostEnvironment env)
        {
            _resend = resend;
            _configuration = configuration;
            _env = env;
        }

        public async Task SendVerificationCodeEmailAsync(string email, string code)
        {
            var htmlBody = await LoadTemplateOrFallback("VerificationCode", code);

            var message = new EmailMessage
            {
                To = email,
                Subject = _configuration["EmailConfiguration:VerificationSubject"]
                          ?? throw new ArgumentNullException("EmailConfiguration:VerificationSubject"),
                From = _configuration["EmailConfiguration:From"]
                       ?? throw new ArgumentNullException("EmailConfiguration:From"),
                HtmlBody = htmlBody
            };

            await _resend.EmailSendAsync(message);
            Log.Information("Verification email enviado a {Email}", email);
        }

        public async Task SendWelcomeEmailAsync(string email)
        {
            var htmlBody = await LoadTemplateOrFallback("Welcome", null);

            var message = new EmailMessage
            {
                To = email,
                Subject = _configuration["EmailConfiguration:WelcomeSubject"]
                          ?? throw new ArgumentNullException("EmailConfiguration:WelcomeSubject"),
                From = _configuration["EmailConfiguration:From"]
                       ?? throw new ArgumentNullException("EmailConfiguration:From"),
                HtmlBody = htmlBody
            };

            await _resend.EmailSendAsync(message);
            Log.Information("Welcome email enviado a {Email}", email);
        }

        private async Task<string> LoadTemplateOrFallback(string templateName, string? code)
        {

            var path = Path.Combine(_env.ContentRootPath, "src", "Application", "Templates", "Email", $"{templateName}.html");
            if (File.Exists(path))
            {
                var html = await File.ReadAllTextAsync(path);
                return html.Replace("{{CODE}}", code ?? "");
            }


            return templateName == "VerificationCode"
                ? $"<h1>Tienda UCN</h1><p>Tu código es: <b>{code}</b></p>"
                : "<h1>¡Bienvenido a Tienda UCN!</h1><p>Gracias por registrarte.</p>";
        }
    }
}