namespace ChatSphere.Infrastructure.Auth;

/// <summary>Signing and lifetime settings for access tokens (bound from the "Jwt" config section).</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "chatsphere";
    public string Audience { get; set; } = "chatsphere-client";
    public int AccessTokenMinutes { get; set; } = 30;
}
