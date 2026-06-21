using ChatSphere.Domain.Common;

namespace ChatSphere.Domain.Entities;

/// <summary>A global user account. Membership in servers is governed by <see cref="ServerMember"/>.</summary>
public class User : Entity
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Hex color for the generated avatar tile, stable per user (e.g. "#7C6CF0").</summary>
    public string AvatarColor { get; set; } = "#7C6CF0";

    /// <summary>Short role/title shown in the member list (e.g. "frontend"). Cosmetic.</summary>
    public string? Title { get; set; }

    // Lockout (brute-force protection).
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedOutUntil { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<ServerMember> ServerMemberships { get; set; } = new List<ServerMember>();

    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public bool IsLockedOut(DateTimeOffset now) => LockedOutUntil is { } until && until > now;

    public void RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= MaxFailedLogins)
        {
            LockedOutUntil = now.Add(LockoutDuration);
            FailedLoginCount = 0;
        }
    }

    public void RegisterSuccessfulLogin()
    {
        FailedLoginCount = 0;
        LockedOutUntil = null;
    }
}
