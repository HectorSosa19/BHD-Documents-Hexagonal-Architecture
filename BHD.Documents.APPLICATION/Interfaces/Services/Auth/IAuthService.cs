using Aplication.DTOs.Requests;
using Aplication.DTOs.Response;
using Aplication.DTOs.Response.Login;

namespace Aplication.Interfaces.Services.Auth;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> LogoutAsync(int userId, CancellationToken cancellationToken = default);
}