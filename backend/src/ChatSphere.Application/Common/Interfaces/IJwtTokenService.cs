using ChatSphere.Domain.Entities;

namespace ChatSphere.Application.Common.Interfaces;

/// <summary>Issues short-lived signed access tokens (JWT) for an authenticated user.</summary>
public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);
}
