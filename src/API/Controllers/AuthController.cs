using Microsoft.AspNetCore.Mvc;
using Serilog;
using Tienda.src.Application.DTO;
using Tienda.src.Application.DTO.AuthDTO;
using Tienda.src.Application.Services.Interfaces;

namespace Tienda.src.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>Inicia sesión y devuelve un JWT.</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            var (token, userId) = await _userService.LoginAsync(loginDTO, HttpContext);
            Log.Information("Login OK para userId={UserId}", userId);
            return Ok(new GenericResponse<string>("Inicio de sesión exitoso", token));
        }

        /// <summary>Registra un usuario (por ahora sin verificación por email).</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {
            var message = await _userService.RegisterAsync(registerDTO, HttpContext);
            return Ok(new GenericResponse<string>("Registro exitoso", message));
        }

        /// <summary>Verifica el correo electrónico (placeholder si aún no implementas códigos).</summary>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDTO verifyEmailDTO)
        {
            var message = await _userService.VerifyEmailAsync(verifyEmailDTO);
            return Ok(new GenericResponse<string>("Verificación procesada", message));
        }

        /// <summary>Reenvía el código de verificación (placeholder si aún no implementas códigos).</summary>
        [HttpPost("resend-email-verification-code")]
        public async Task<IActionResult> ResendEmailVerificationCode([FromBody] ResendEmailVerificationCodeDTO dto)
        {
            var message = await _userService.ResendEmailVerificationCodeAsync(dto);
            return Ok(new GenericResponse<string>("Solicitud procesada", message));
        }
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // El cliente debe borrar el JWT; aquí solo respondemos OK.
            return Ok(new GenericResponse<string>("Sesión cerrada", "Logout exitoso."));
        }

        [HttpPost("recover-password")]
        public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordDTO dto)
        {
            var msg = await _userService.RecoverPasswordAsync(dto.Email);
            return Ok(new GenericResponse<string>("Solicitud procesada", msg));
        }

        [HttpPatch("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            var msg = await _userService.ResetPasswordAsync(dto);
            return Ok(new GenericResponse<string>("Contraseña restablecida", msg));
        }
    }

}