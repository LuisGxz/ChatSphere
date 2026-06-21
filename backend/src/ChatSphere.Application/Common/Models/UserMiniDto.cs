namespace ChatSphere.Application.Common.Models;

/// <summary>Compact user projection for authors, members and DM partners.</summary>
public record UserMiniDto(Guid Id, string DisplayName, string AvatarColor, string? Title);
