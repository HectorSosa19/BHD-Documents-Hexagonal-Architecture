using System.Security.Claims;
using Aplication.DTOs.Requests;
using Aplication.DTOs.Response;
using Aplication.DTOs.Response.Login;
using Aplication.Interfaces.Services.Auth;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/bhd/mgmt/1/auth/")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
     private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Datos de registro inválidos",
                    Errors = errors
                });
            }

            try
            {
                var result = await _authService.RegisterAsync(request, cancellationToken);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Usuario {Username} registrado exitosamente", request.Username);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario {Username}", request.Username);
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Error interno del servidor al registrar usuario"
                });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Datos de login inválidos",
                    Errors = errors
                });
            }

            try
            {
                var result = await _authService.LoginAsync(request, cancellationToken);

                if (!result.Success)
                {
                    return Unauthorized(result);
                }

                _logger.LogInformation("Usuario {Email} inició sesión exitosamente", request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al hacer login para {Email}", request.Email);
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Error interno del servidor al iniciar sesión"
                });
            }
        }
        
        [HttpPost("refreshToken")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshToken(
            [FromBody] RefreshTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Refresh token no proporcionado"
                });
            }

            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Token renovado exitosamente");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al renovar token");
                return StatusCode(500, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Error interno del servidor al renovar token"
                });
            }
        }


        [HttpPost("revoke")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RevokeToken(
            [FromBody] RefreshTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Refresh token no proporcionado"
                });
            }

            try
            {
                var result = await _authService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revocar token");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Error interno del servidor al revocar token"
                });
            }
        }


        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Usuario no autenticado"
                });
            }

            try
            {
                var result = await _authService.LogoutAsync(userId, cancellationToken);
                
                _logger.LogInformation("Usuario {UserId} cerró sesión", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesión para usuario {UserId}", userId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Error interno del servidor al cerrar sesión"
                });
            }
        }


        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Usuario no autenticado"
                });
            }

            var userDto = new UserDto
            {
                Id = int.Parse(userIdClaim),
                Username = username ?? "",
                Email = email ?? "",
                CreatedAt = DateTime.UtcNow 
            };

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Usuario obtenido exitosamente",
                Data = userDto
            });
        }
}
