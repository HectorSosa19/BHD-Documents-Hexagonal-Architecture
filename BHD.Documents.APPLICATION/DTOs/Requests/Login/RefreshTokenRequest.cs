using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs.Requests;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "El refresh token es requerido")]
    public string RefreshToken { get; set; } = string.Empty;
}