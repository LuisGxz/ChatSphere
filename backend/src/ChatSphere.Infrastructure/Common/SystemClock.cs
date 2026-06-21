using ChatSphere.Application.Common.Interfaces;

namespace ChatSphere.Infrastructure.Common;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
