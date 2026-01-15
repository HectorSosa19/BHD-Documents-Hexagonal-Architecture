using Domain.Entities;
using Domain.Repositories;
using Infraestructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DataContext _context;

        public RefreshTokenRepository(DataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        }

        public async Task<List<RefreshToken>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<RefreshToken>> GetActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && 
                            !rt.IsRevoked && 
                            rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }

        public async Task<RefreshToken> AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return refreshToken;
        }

        public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var token = await _context.RefreshTokens.FindAsync(new object[] { id }, cancellationToken);
            if (token != null)
            {
                _context.RefreshTokens.Remove(token);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RevokeByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
            
            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RevokeAllByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task CleanExpiredAsync(CancellationToken cancellationToken = default)
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
