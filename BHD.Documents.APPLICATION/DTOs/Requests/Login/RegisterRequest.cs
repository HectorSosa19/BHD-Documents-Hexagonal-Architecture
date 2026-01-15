using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs.Requests;

public class RegisterRequest
{
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = string.Empty;
}