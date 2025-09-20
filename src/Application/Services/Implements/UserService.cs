using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using Tienda.src.Application.DTO;
using Tienda.src.Application.DTO.AuthDTO;
using Tienda.src.Application.Services.Interfaces;
using Tienda.src.Domain.Models;

namespace Tienda.src.Application.Services.Implements
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public UserService(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
        }

        // -------- LOGIN --------
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

            // loginDTO.RememberMe es bool (no nullable)
            var rememberMe = loginDTO.RememberMe;

            var token = _tokenService.GenerateToken(user, roleName, rememberMe);
            Log.Information("Usuario {Email} inició sesión", user.Email);
            return (token, user.Id);
        }

        // -------- REGISTER (sin verificación, deja EmailConfirmed=true) --------
        public async Task<string> RegisterAsync(RegisterDTO dto, HttpContext httpContext)
        {
            // Unicidad por Email y RUT
            if (await _userManager.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("El email ya está registrado");

            if (await _userManager.Users.AnyAsync(u => u.Rut == dto.Rut))
                throw new InvalidOperationException("El RUT ya está registrado");

            var user = new User
            {
                Email = dto.Email,
                UserName = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Rut = dto.Rut,
                Gender = ParseGender(dto.Gender),        // <-- mapeo string -> enum
                BirthDate = dto.BirthDate,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = true                    // <-- por ahora confirmamos directo
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errs = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"No se pudo crear el usuario: {errs}");
            }

            // rol por defecto
            if (!await _roleManager.RoleExistsAsync("Customer"))
                await _roleManager.CreateAsync(new Role { Name = "Customer", NormalizedName = "CUSTOMER" });

            await _userManager.AddToRoleAsync(user, "Customer");

            // Email de bienvenida (fake o real según tu EmailService)
            try { await _emailService.SendWelcomeEmailAsync(user.Email!); } catch { /* opcional: loggear */ }

            Log.Information("Usuario {Email} registrado (email confirmado por defecto).", user.Email);
            return "Usuario registrado correctamente.";
        }

        // -------- VERIFY EMAIL (placeholder) --------
        public Task<string> VerifyEmailAsync(VerifyEmailDTO verifyEmailDTO)
        {
            // Placeholder mientras no tengas VerificationCode
            return Task.FromResult("La verificación por email aún no está habilitada en este entorno.");
        }

        // -------- RESEND CODE (placeholder) --------
        public Task<string> ResendEmailVerificationCodeAsync(ResendEmailVerificationCodeDTO resendEmailVerificationCodeDTO)
        {
            // Placeholder mientras no tengas VerificationCode
            return Task.FromResult("El reenvío de código aún no está habilitado en este entorno.");
        }

        // -------- DELETE UNCONFIRMED --------
        public async Task<int> DeleteUnconfirmedAsync()
        {
            var toDelete = await _userManager.Users
                .Where(u => !u.EmailConfirmed)
                .ToListAsync();

            var count = 0;
            foreach (var u in toDelete)
            {
                var res = await _userManager.DeleteAsync(u);
                if (res.Succeeded) count++;
            }
            Log.Information("Usuarios no confirmados eliminados: {Count}", count);
            return count;
        }

        private static Gender ParseGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Gender.Otro;

            // intenta parseo directo respetando nombres de tu enum (Masculino, Femenino, Otro)
            if (Enum.TryParse<Gender>(value, ignoreCase: true, out var g))
                return g;

            // mapeos opcionales por si te llegan variantes
            var v = value.Trim().ToLowerInvariant();
            if (v.StartsWith("masc")) return Gender.Masculino;
            if (v.StartsWith("fem")) return Gender.Femenino;

            return Gender.Otro;
        }
    }
}