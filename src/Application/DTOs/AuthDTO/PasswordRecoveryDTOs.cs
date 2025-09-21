using System.ComponentModel.DataAnnotations;

namespace Tienda.src.Application.DTO.AuthDTO
{
    public class RecoverPasswordDTO
    {
        [Required, EmailAddress]
        public required string Email { get; set; }
    }

    public class ResetPasswordDTO
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required, RegularExpression(@"^\d{6}$")]
        public required string VerificationCode { get; set; }

        [Required, MinLength(8)]
        public required string NewPassword { get; set; }

        [Required, Compare(nameof(NewPassword))]
        public required string ConfirmNewPassword { get; set; }
    }
}