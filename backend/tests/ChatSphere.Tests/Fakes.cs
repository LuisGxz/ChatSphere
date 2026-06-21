using ChatSphere.Application.Common.Interfaces;
using ChatSphere.Domain.Entities;

namespace ChatSphere.Tests;

internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

internal sealed class FakeJwt(IClock clock) : IJwtTokenService
{
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
        => ($"access-{user.Id}", clock.UtcNow.AddMinutes(30));
}
