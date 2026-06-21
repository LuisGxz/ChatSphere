namespace ChatSphere.Application.Common.Interfaces;

/// <summary>Generates and hashes opaque refresh tokens; only the SHA-256 hash is persisted.</summary>
public interface ITokenHasher
{
    string GenerateRawToken();
    string Hash(string rawToken);
}
