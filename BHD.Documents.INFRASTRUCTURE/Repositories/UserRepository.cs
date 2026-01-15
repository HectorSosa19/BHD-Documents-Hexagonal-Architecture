using Domain.Entities;
using Domain.Repositories;
using Infraestructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DataContext _context;

        public UserRepository(DataContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        }

        public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Username == username, cancellationToken);
        }

        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
}