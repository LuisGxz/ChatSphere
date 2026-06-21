using ChatSphere.Application.Common;
using ChatSphere.Application.Common.Exceptions;
using ChatSphere.Application.Common.Interfaces;
using ChatSphere.Domain.Entities;
using ChatSphere.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChatSphere.Application.Auth;

/// <summary>
/// Account lifecycle: registration (new accounts auto-join the demo server so the experience is immediate),
/// password login with brute-force lockout, and refresh-token rotation (old token revoked + linked).
/// </summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwt,
    ITokenHasher tokenHasher,
    IClock clock,
    IOptions<AuthSettings> authSettings,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator) : IAuthService
{
    private readonly AuthSettings _auth = authSettings.Value;

    private static readonly string[] Palette =
        ["#7C6CF0", "#B05CCB", "#3E8FD9", "#4CAF82", "#C8537B", "#D9763E", "#5BA8A0", "#8A8F9E"];

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("That email is already registered.", "email_taken");

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            AvatarColor = Palette[Math.Abs(StableHash(email)) % Palette.Length],
        };
        user.PasswordHash = passwordHasher.Hash(user, request.Password);
        db.Users.Add(user);

        await AutoJoinDemoServerAsync(user.Id, ct);

        var tokens = await IssueTokensAsync(user, ct);
        await db.SaveChangesAsync(ct);
        return new AuthResponse(ToDto(user), tokens);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
            throw new UnauthorizedException("Invalid email or password.", "invalid_credentials");

        var now = clock.UtcNow;
        if (user.IsLockedOut(now))
            throw new UnauthorizedException("Account temporarily locked. Try again later.", "locked_out");

        if (!passwordHasher.Verify(user, user.PasswordHash, request.Password))
        {
            user.RegisterFailedLogin(now);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid email or password.", "invalid_credentials");
        }

        user.RegisterSuccessfulLogin();
        var tokens = await IssueTokensAsync(user, ct);
        await db.SaveChangesAsync(ct);
        return new AuthResponse(ToDto(user), tokens);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        await refreshValidator.ValidateAndThrowAsync(request, ct);

        var hash = tokenHasher.Hash(request.RefreshToken);
        var token = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        var now = clock.UtcNow;
        if (token is null || token.User is null || !token.IsActive(now))
            throw new UnauthorizedException("Invalid or expired refresh token.", "invalid_refresh_token");

        var tokens = await IssueTokensAsync(token.User, ct);
        token.Revoke(now, tokenHasher.Hash(tokens.RefreshToken));
        await db.SaveChangesAsync(ct);
        return new AuthResponse(ToDto(token.User), tokens);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return;
        var hash = tokenHasher.Hash(request.RefreshToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is { RevokedAt: null })
        {
            token.Revoke(clock.UtcNow);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<MeResponse> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException("User not found.");
        return new MeResponse(ToDto(user));
    }

    // ---- helpers ----

    private async Task AutoJoinDemoServerAsync(Guid userId, CancellationToken ct)
    {
        // New sign-ups land in the seeded community so the demo is usable immediately.
        var server = await db.Servers.OrderBy(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        if (server is null)
            return;

        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = userId, Role = ServerRole.Member });

        var publicChannels = await db.Channels
            .Where(c => c.ServerId == server.Id && !c.IsPrivate && !c.IsDirect)
            .Select(c => c.Id)
            .ToListAsync(ct);
        foreach (var channelId in publicChannels)
            db.ChannelMembers.Add(new ChannelMember { ChannelId = channelId, UserId = userId });
    }

    private async Task<AuthTokens> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (accessToken, accessExpires) = jwt.CreateAccessToken(user);
        var rawRefresh = tokenHasher.GenerateRawToken();
        var refreshExpires = clock.UtcNow.AddDays(_auth.RefreshTokenDays);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHasher.Hash(rawRefresh),
            ExpiresAt = refreshExpires,
        });
        await Task.CompletedTask;
        return new AuthTokens(accessToken, accessExpires, rawRefresh, refreshExpires);
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Email, u.DisplayName, u.AvatarColor, u.Title);

    private static int StableHash(string s)
    {
        var h = 0;
        foreach (var ch in s) h = unchecked(h * 31 + ch);
        return h;
    }
}
