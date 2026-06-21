using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatSphere.Infrastructure.Data;

/// <summary>Lets `dotnet ef` build the context at design time without booting the API.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChatSphereDbContext>
{
    public ChatSphereDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ChatSphereDbContext>()
            .UseSqlServer("Server=localhost;Database=ChatSphere;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new ChatSphereDbContext(options);
    }
}
