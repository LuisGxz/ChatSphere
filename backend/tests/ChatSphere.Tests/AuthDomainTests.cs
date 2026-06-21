using ChatSphere.Domain.Entities;

namespace ChatSphere.Tests;

public class AuthDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Lockout_ReachingThreshold_LocksAndResetsCounter()
    {
        var user = new User();
        for (var i = 0; i < User.MaxFailedLogins; i++)
            user.RegisterFailedLogin(Now);

        Assert.True(user.IsLockedOut(Now));
        Assert.Equal(0, user.FailedLoginCount);
        Assert.False(user.IsLockedOut(Now.Add(User.LockoutDuration).AddSeconds(1)));
    }

    [Fact]
    public void SuccessfulLogin_ClearsFailuresAndLock()
    {
        var user = new User { FailedLoginCount = 3, LockedOutUntil = Now.AddMinutes(10) };
        user.RegisterSuccessfulLogin();

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockedOutUntil);
        Assert.False(user.IsLockedOut(Now));
    }

    [Fact]
    public void RefreshToken_Active_WhenNotRevokedAndNotExpired()
    {
        var token = new RefreshToken { ExpiresAt = Now.AddDays(1) };
        Assert.True(token.IsActive(Now));
        Assert.False(token.IsActive(Now.AddDays(2)));
    }

    [Fact]
    public void RefreshToken_Revoke_MarksInactive_AndRecordsReplacement()
    {
        var token = new RefreshToken { ExpiresAt = Now.AddDays(1) };
        token.Revoke(Now, "next-hash");

        Assert.False(token.IsActive(Now));
        Assert.Equal("next-hash", token.ReplacedByTokenHash);
        Assert.NotNull(token.RevokedAt);
    }
}
