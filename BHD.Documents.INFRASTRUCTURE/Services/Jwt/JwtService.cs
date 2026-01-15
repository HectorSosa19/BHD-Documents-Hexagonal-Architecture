using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infraestructure.Services.Jwt;

public class JwtService : IJwtService
{
     private readonly IConfiguration _configuration;
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpirationMinutes;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            _secret = _configuration["Jwt:Secret"] 
                ?? throw new InvalidOperationException("JWT Secret no configurado en appsettings.json");
            
            _issuer = _configuration["Jwt:Issuer"] 
                ?? throw new InvalidOperationException("JWT Issuer no configurado");
            
            _audience = _configuration["Jwt:Audience"] 
                ?? throw new InvalidOperationException("JWT Audience no configurado");
            
            _accessTokenExpirationMinutes = int.Parse(
                _configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15"
            );
        }

        public string GenerateAccessToken(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero 
                }, out SecurityToken validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                return null;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public int? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null)
            {
                return null;
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }


        public string? GetUserEmailFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null)
            {
                return null;
            }

            return principal.FindFirst(ClaimTypes.Email)?.Value 
                ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        }
    }
