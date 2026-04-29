using CivicDesk.API.Controllers;
using CivicDesk.API.DTOs;
using CivicDesk.API.Models;
using CivicDesk.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CivicDesk.Tests.Controllers;

public class AuthControllerTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]          = "civicdesk-test-secret-key-must-be-32chars!!",
                ["Jwt:Issuer"]          = "CivicDesk",
                ["Jwt:Audience"]        = "CivicDesk",
                ["Jwt:ExpiresInHours"]  = "24"
            })
            .Build();

    // ── Admin Login ───────────────────────────────────────────────────────────

    [Fact]
    public void Login_ReturnsOk_WithToken_WhenCredentialsAreValid()
    {
        using var db = DbContextFactory.Create();
        db.AdminUsers.Add(new AdminUser
        {
            Username     = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            CreatedAt    = DateTime.UtcNow
        });
        db.SaveChanges();
        var controller = new AuthController(db, BuildConfig());

        var result = controller.Login(new LoginDto("admin", "Admin123!"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var token = Assert.IsType<AuthTokenDto>(ok.Value);
        Assert.False(string.IsNullOrEmpty(token.Token));
        Assert.True(token.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void Login_ReturnsUnauthorized_WhenPasswordIsWrong()
    {
        using var db = DbContextFactory.Create();
        db.AdminUsers.Add(new AdminUser
        {
            Username     = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            CreatedAt    = DateTime.UtcNow
        });
        db.SaveChanges();
        var controller = new AuthController(db, BuildConfig());

        var result = controller.Login(new LoginDto("admin", "wrongpassword"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Login_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        using var db = DbContextFactory.Create();
        var controller = new AuthController(db, BuildConfig());

        var result = controller.Login(new LoginDto("nonexistent", "anything"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ── Resident Login ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResidentLogin_ReturnsOk_WithToken_WhenEmailAndReferenceMatch()
    {
        using var db = DbContextFactory.Create();
        db.ServiceRequests.Add(new ServiceRequest
        {
            ReferenceNumber   = "POT-20260429-ABCDEF",
            Type              = RequestType.Pothole,
            FullName          = "Resident",
            Email             = "resident@test.com",
            AddressOrLocation = "10 High St",
            Description       = "Pothole outside house",
            CreatedAt         = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var controller = new AuthController(db, BuildConfig());

        var result = await controller.ResidentLogin(
            new ResidentLoginDto("resident@test.com", "POT-20260429-ABCDEF"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var token = Assert.IsType<AuthTokenDto>(ok.Value);
        Assert.False(string.IsNullOrEmpty(token.Token));
    }

    [Fact]
    public async Task ResidentLogin_ReturnsUnauthorized_WhenNoMatchFound()
    {
        using var db = DbContextFactory.Create();
        var controller = new AuthController(db, BuildConfig());

        var result = await controller.ResidentLogin(
            new ResidentLoginDto("nobody@test.com", "FAKE-REF-000"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ResidentLogin_ReturnsUnauthorized_WhenEmailMatchesButReferenceDoesNot()
    {
        using var db = DbContextFactory.Create();
        db.ServiceRequests.Add(new ServiceRequest
        {
            ReferenceNumber   = "POT-20260429-ABCDEF",
            Type              = RequestType.Pothole,
            FullName          = "Resident",
            Email             = "resident@test.com",
            AddressOrLocation = "10 High St",
            Description       = "Pothole",
            CreatedAt         = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var controller = new AuthController(db, BuildConfig());

        var result = await controller.ResidentLogin(
            new ResidentLoginDto("resident@test.com", "WRONG-REF-000"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
