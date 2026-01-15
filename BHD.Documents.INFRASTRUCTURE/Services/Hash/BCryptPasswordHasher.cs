using Domain.Repositories;

namespace Aplication.Interfaces.Services.Auth;

public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 10;
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        if (string.IsNullOrEmpty(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be null or empty", nameof(passwordHash));
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
    
}