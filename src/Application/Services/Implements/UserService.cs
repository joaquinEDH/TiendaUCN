using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Tienda.src.Application.DTO;
using Tienda.src.Application.DTO.AuthDTO;
using Tienda.src.Application.Services.Interfaces;
using Tienda.src.Domain.Models;
using Tienda.src.Infrastructure.Repositories.Interfaces;

namespace Tienda.src.Application.Services.Implements
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IUserRepository _userRepository;
        private readonly IVerificationCodeRepository _verificationCodeRepository;

        public UserService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration config,
            IUserRepository userRepository,
            IVerificationCodeRepository verificationCodeRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _userRepository = userRepository;
            _config = config;
            _verificationCodeRepository = verificationCodeRepository;
        }

        // ---------- LOGIN ----------
        public async Task<(string token, int userId)> LoginAsync(LoginDTO loginDTO, HttpContext httpContext)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == loginDTO.Email);
            if (user == null)
                throw new UnauthorizedAccessException("Credenciales inválidas");

            if (!user.EmailConfirmed)
                throw new InvalidOperationException("Debes verificar tu email antes de iniciar sesión");

            var valid = await _userManager.CheckPasswordAsync(user, loginDTO.Password);
            if (!valid)
                throw new UnauthorizedAccessException("Credenciales inválidas");

            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault() ?? "Customer";

            var token = _tokenService.GenerateToken(user, roleName, loginDTO.RememberMe);
            Log.Information("Usuario {Email} inició sesión", user.Email);
            return (token, user.Id);
        }

        // ---------- REGISTER ----------
        public async Task<string> RegisterAsync(RegisterDTO dto, HttpContext httpContext)
        {


            if (await _userManager.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("El email ya está registrado");

            if (await _userManager.Users.AnyAsync(u => u.Rut == dto.Rut))
                throw new InvalidOperationException("El RUT ya está registrado");

            var user = dto.Adapt<User>();

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errs = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"No se pudo crear el usuario: {errs}");
            }

            if (!await _roleManager.RoleExistsAsync("Customer"))
                await _roleManager.CreateAsync(new Role { Name = "Customer", NormalizedName = "CUSTOMER" });
            await _userManager.AddToRoleAsync(user, "Customer");

            // Crear y enviar código
            var expiryMin = _config.GetValue<int?>("VerificationCode:ExpirationTimeInMinutes") ?? 3;
            var code = new Random().Next(100000, 999999).ToString();

            var vc = new VerificationCode
            {
                UserId = user.Id,
                Code = code,
                CodeType = CodeType.EmailVerification,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMinutes(expiryMin)
            };

            await _verificationCodeRepository.CreateAsync(vc);
            try
            {
                await _emailService.SendVerificationCodeEmailAsync(user.Email!, code);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[DEV] Falló envío de email. Continuando. Code={Code}", code);

                if (_config["ASPNETCORE_ENVIRONMENT"] == "Development")
                {
                }
            }

            Log.Information("Usuario {Email} registrado. Código enviado.", user.Email);
            return "Usuario registrado. Revisa tu email para verificar la cuenta.";
        }

        // ---------- VERIFY EMAIL ----------
        public async Task<string> VerifyEmailAsync(VerifyEmailDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) throw new KeyNotFoundException("Usuario no encontrado");

            if (user.EmailConfirmed)
                throw new InvalidOperationException("El email ya está verificado");

            var lastCode = await _verificationCodeRepository.GetLatestByUserIdAsync(user.Id, CodeType.EmailVerification);
            if (lastCode is null)
                throw new InvalidOperationException("Código de verificación no encontrado");

            // Check expiración
            if (DateTime.UtcNow >= lastCode.ExpiryDate)
            {
                await _verificationCodeRepository.IncreaseAttemptsAsync(user.Id, CodeType.EmailVerification);
                throw new InvalidOperationException("Código expirado");
            }

            // Check match
            if (!string.Equals(dto.VerificationCode, lastCode.Code, StringComparison.Ordinal))
            {
                var attempts = await _verificationCodeRepository.IncreaseAttemptsAsync(user.Id, CodeType.EmailVerification);
                if (attempts >= 5)
                {
                    await _verificationCodeRepository.DeleteByUserIdAsync(user.Id);
                    await _userManager.DeleteAsync(user);
                    throw new InvalidOperationException("Demasiados intentos fallidos. Cuenta eliminada.");
                }
                throw new InvalidOperationException("Código inválido");
            }

            // Confirmar email + limpiar códigos + welcome
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            await _verificationCodeRepository.DeleteByUserIdAsync(user.Id);

            try { await _emailService.SendWelcomeEmailAsync(user.Email!); } catch { /* log opcional */ }

            Log.Information("Email verificado para {Email}", user.Email);
            return "Email verificado exitosamente. ¡Ya puedes iniciar sesión!";
        }

        // ---------- RESEND CODE ----------
        public async Task<string> ResendEmailVerificationCodeAsync(ResendEmailVerificationCodeDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) throw new InvalidOperationException("Usuario no encontrado");

            if (user.EmailConfirmed)
                throw new InvalidOperationException("El email ya está verificado");

            var expiryMin = _config.GetValue<int?>("VerificationCode:ExpirationTimeInMinutes") ?? 3;
            var lastCode = await _verificationCodeRepository.GetLatestByUserIdAsync(user.Id, CodeType.EmailVerification);

            // Rate limit: si aún no venció, no reenviar
            if (lastCode != null && DateTime.UtcNow < lastCode.ExpiryDate)
            {
                var remaining = (int)(lastCode.ExpiryDate - DateTime.UtcNow).TotalSeconds;
                throw new InvalidOperationException($"Debes esperar {remaining} segundos para solicitar otro código.");
            }

            var newCode = new Random().Next(100000, 999999).ToString();
            var vc = new VerificationCode
            {
                UserId = user.Id,
                Code = newCode,
                CodeType = CodeType.EmailVerification,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMinutes(expiryMin)
            };

            await _verificationCodeRepository.CreateAsync(vc);
            await _emailService.SendVerificationCodeEmailAsync(user.Email!, newCode);

            Log.Information("Código reenviado a {Email}", user.Email);
            return "Código de verificación reenviado a tu email";
        }

        // ---------- RECOVER PASSWORD ----------
        public async Task<string> RecoverPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);


            if (user is null)
                return "Si el correo existe, se enviará un código de recuperación.";

            if (!user.EmailConfirmed)
                throw new InvalidOperationException("Debes verificar tu email antes de recuperar la contraseña.");

            var expiryMin = _config.GetValue<int?>("VerificationCode:ExpirationTimeInMinutes") ?? 3;

            // Rate limit: si el último código de PasswordReset aún no vence, no se genera otro
            var last = await _verificationCodeRepository
                .GetLatestByUserIdAsync(user.Id, CodeType.PasswordReset);

            if (last != null && DateTime.UtcNow < last.ExpiryDate)
            {
                var remaining = (int)(last.ExpiryDate - DateTime.UtcNow).TotalSeconds;
                throw new InvalidOperationException($"Debes esperar {remaining} segundos para solicitar otro código.");
            }

            // Nuevo código de 6 dígitos
            var code = new Random().Next(100000, 999999).ToString();

            var vc = new VerificationCode
            {
                UserId = user.Id,
                Code = code,
                CodeType = CodeType.PasswordReset,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMinutes(expiryMin)
            };

            await _verificationCodeRepository.CreateAsync(vc);

            // Envía correo 
            await _emailService.SendVerificationCodeEmailAsync(user.Email!, code);

            Serilog.Log.Information("Código de recuperación enviado a {Email}", email);
            return "Si el correo existe, se enviará un código de recuperación.";
        }

        // ---------- RESET PASSWORD ----------
        public async Task<string> ResetPasswordAsync(ResetPasswordDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                throw new InvalidOperationException("Usuario no encontrado.");

            if (!user.EmailConfirmed)
                throw new InvalidOperationException("Debes verificar tu email antes de recuperar la contraseña.");

            var lastCode = await _verificationCodeRepository
                .GetLatestByUserIdAsync(user.Id, CodeType.PasswordReset);

            if (lastCode is null)
                throw new InvalidOperationException("Código de recuperación no encontrado.");

            // Expirado
            if (DateTime.UtcNow >= lastCode.ExpiryDate)
            {
                await _verificationCodeRepository.IncreaseAttemptsAsync(user.Id, CodeType.PasswordReset);
                throw new InvalidOperationException("Código expirado.");
            }

            // No coincide
            if (!string.Equals(dto.VerificationCode, lastCode.Code, StringComparison.Ordinal))
            {
                var attempts = await _verificationCodeRepository.IncreaseAttemptsAsync(user.Id, CodeType.PasswordReset);
                if (attempts >= 5)
                {
                    await _verificationCodeRepository.DeleteByUserIdAsync(user.Id, CodeType.PasswordReset);
                    throw new InvalidOperationException("Demasiados intentos fallidos.");
                }
                throw new InvalidOperationException("Código inválido.");
            }

            // Cambiar la contraseña usando Identity (token interno + hash seguro)
            var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, identityToken, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errs = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"No se pudo actualizar la contraseña: {errs}");
            }

            // Limpia códigos de PasswordReset
            await _verificationCodeRepository.DeleteByUserIdAsync(user.Id, CodeType.PasswordReset);

            Serilog.Log.Information("Contraseña restablecida para {Email}", user.Email);
            return "Contraseña restablecida correctamente.";
        }

        public async Task<int> DeleteUnconfirmedAsync()
        {
            return await _userRepository.DeleteUnconfirmedAsync();
        }

        private static Gender ParseGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Gender.Otro;
            if (Enum.TryParse<Gender>(value, true, out var g)) return g;
            var v = value.Trim().ToLowerInvariant();
            if (v.StartsWith("masc")) return Gender.Masculino;
            if (v.StartsWith("fem")) return Gender.Femenino;
            return Gender.Otro;
        }
    }
}