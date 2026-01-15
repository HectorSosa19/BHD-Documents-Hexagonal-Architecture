using Domain.Entities;

namespace Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        
    Task<RefreshToken> AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task RevokeByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAllByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task CleanExpiredAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}