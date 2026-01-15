using Aplication.DTOs.Requests;
using Aplication.DTOs.Response;
using Aplication.DTOs.Response.Login;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Configuration;

namespace Aplication.Interfaces.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IConfiguration configuration)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(
            RegisterRequest request, 
            CancellationToken cancellationToken = default)
        {
            if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "El email ya está registrado",
                    Errors = new List<string> { "Email duplicado" }
                };
            }

            if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "El nombre de usuario ya está en uso",
                    Errors = new List<string> { "Username duplicado" }
                };
            }

            var passwordHash = _passwordHasher.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            user = await _userRepository.AddAsync(user, cancellationToken);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenValue = _jwtService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")
                ),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

            var authResponse = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15")
                ),
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            };

            return new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Usuario registrado exitosamente",
                Data = authResponse
            };
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(
            LoginRequest request, 
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user == null)
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Credenciales inválidas",
                    Errors = new List<string> { "Email o contraseña incorrectos" }
                };
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Credenciales inválidas",
                    Errors = new List<string> { "Email o contraseña incorrectos" }
                };
            }

            await _userRepository.UpdateLastLoginAsync(user.Id, cancellationToken);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshTokenValue = _jwtService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")
                ),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

            var authResponse = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15")
                ),
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            };

            return new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Login exitoso",
                Data = authResponse
            };
        }

        public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(
            string refreshToken, 
            CancellationToken cancellationToken = default)
        {
            var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);

            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Refresh token inválido o expirado",
                    Errors = new List<string> { "Token inválido" }
                };
            }

            await _refreshTokenRepository.RevokeByTokenAsync(refreshToken, cancellationToken);

            var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
            if (user == null)
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Usuario no encontrado"
                };
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshTokenValue = _jwtService.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenValue,
                UserId = storedToken.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")
                ),
                CreatedAt = DateTime.UtcNow
            };

            await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

            var authResponse = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15")
                ),
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                }
            };

            return new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Token renovado exitosamente",
                Data = authResponse
            };
        }

        public async Task<ApiResponse<bool>> RevokeTokenAsync(
            string refreshToken, 
            CancellationToken cancellationToken = default)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);

            if (token == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Token no encontrado",
                    Data = false
                };
            }

            await _refreshTokenRepository.RevokeByTokenAsync(refreshToken, cancellationToken);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Token revocado exitosamente",
                Data = true
            };
        }

        public async Task<ApiResponse<bool>> LogoutAsync(
            int userId, 
            CancellationToken cancellationToken = default)
        {
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Sesión cerrada exitosamente",
                Data = true
            };
        }
    }
