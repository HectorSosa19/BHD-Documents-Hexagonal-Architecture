using System.Security.Claims;
using Domain.Entities;

namespace Domain.Repositories;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    int? GetUserIdFromToken(string token);
    string? GetUserEmailFromToken(string token);
}