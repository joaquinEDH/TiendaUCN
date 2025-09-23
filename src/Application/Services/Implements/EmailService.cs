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
            var htmlBody = await LoadTemplate("VerificationCode", code);

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
            var htmlBody = await LoadTemplate("Welcome", null);

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


        /// <summary>
        /// Carga una plantilla de correo electrónico desde el sistema de archivos y reemplaza el marcador de código.
        /// </summary>
        /// <param name="templateName">El nombre de la plantilla sin extensión.</param>
        /// <param name="code">El código a insertar en la plantilla.</param>
        /// <returns>El contenido HTML de la plantilla con el código reemplazado.</returns
        private async Task<string> LoadTemplate(string templateName, string? code)
        {
            var templatePath = Path.Combine(_env.ContentRootPath, "Src", "Application", "Templates", "Email", $"{templateName}.html");
            var html = await File.ReadAllTextAsync(templatePath);
            return html.Replace("{{CODE}}", code);
        }
    }
}