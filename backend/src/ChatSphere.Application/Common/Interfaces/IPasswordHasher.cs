using ChatSphere.Domain.Entities;

namespace ChatSphere.Application.Common.Interfaces;

/// <summary>Hashes and verifies user passwords (adapter over ASP.NET Identity's PasswordHasher).</summary>
public interface IPasswordHasher
{
    string Hash(User user, string password);
    bool Verify(User user, string passwordHash, string password);
}
