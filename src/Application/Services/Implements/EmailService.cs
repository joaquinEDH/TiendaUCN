using Serilog;
using Tienda.src.Application.Services.Interfaces;

namespace Tienda.src.Application.Services.Implements
{
    public class EmailService : IEmailService
    {
        public Task SendVerificationCodeEmailAsync(string email, string code)
        {
            Log.Information("[FAKE EMAIL] To={Email} Code={Code}", email, code);
            return Task.CompletedTask;
        }

        public Task SendWelcomeEmailAsync(string email)
        {
            Log.Information("[FAKE EMAIL] Welcome To={Email}", email);
            return Task.CompletedTask;
        }
    }
}