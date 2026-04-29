using CivicDesk.API.DTOs;
using CivicDesk.API.Models;
using CivicDesk.API.Services;
using CivicDesk.Tests.Helpers;

namespace CivicDesk.Tests.Services;

public class ServiceRequestServiceTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsDto_WithCorrectFields()
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);

        var result = await service.CreateAsync(new CreateServiceRequestDto(
            RequestType.Pothole, "John Doe", "john@example.com",
            "123 Main St", "Large pothole"));

        Assert.Equal(RequestType.Pothole, result.Type);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("123 Main St", result.AddressOrLocation);
        Assert.Equal("Large pothole", result.Description);
        Assert.Equal(RequestStatus.Submitted, result.Status);
        Assert.Null(result.AdminNotes);
        Assert.True(result.Id > 0);
    }

    [Theory]
    [InlineData(RequestType.Pothole,        "POT")]
    [InlineData(RequestType.MissedBin,      "BIN")]
    [InlineData(RequestType.NoiseComplaint, "NOI")]
    [InlineData(RequestType.PlanningQuery,  "PLN")]
    [InlineData(RequestType.StreetLighting, "STL")]
    [InlineData(RequestType.Other,          "OTH")]
    public async Task CreateAsync_GeneratesReference_WithCorrectTypePrefix(
        RequestType type, string expectedPrefix)
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);

        var result = await service.CreateAsync(new CreateServiceRequestDto(
            type, "Jane", "jane@test.com", "Somewhere", "Issue"));

        Assert.StartsWith($"{expectedPrefix}-", result.ReferenceNumber);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenExists()
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);
        var created = await service.CreateAsync(new CreateServiceRequestDto(
            RequestType.MissedBin, "Alice", "alice@test.com", "5 Park Lane", "Missed collection"));

        var result = await service.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Alice", result.FullName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // ── GetByReference ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByReferenceAsync_ReturnsDto_WhenExists()
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);
        var created = await service.CreateAsync(new CreateServiceRequestDto(
            RequestType.Pothole, "Bob", "bob@test.com", "High St", "Pothole"));

        var result = await service.GetByReferenceAsync(created.ReferenceNumber);

        Assert.NotNull(result);
        Assert.Equal(created.ReferenceNumber, result.ReferenceNumber);
    }

    [Fact]
    public async Task GetByReferenceAsync_ReturnsNull_WhenNotFound()
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);

        var result = await service.GetByReferenceAsync("FAKE-REF-000");

        Assert.Null(result);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsRequests_OrderedByCreatedAtDescending()
    {
        using var db = DbContextFactory.Create();
        db.ServiceRequests.AddRange(
            new ServiceRequest { ReferenceNumber = "POT-AAA", Type = RequestType.Pothole,        FullName = "A", Email = "a@t.com", AddressOrLocation = "Addr", Description = "Desc", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new ServiceRequest { ReferenceNumber = "BIN-BBB", Type = RequestType.MissedBin,      FullName = "B", Email = "b@t.com", AddressOrLocation = "Addr", Description = "Desc", CreatedAt = DateTime.UtcNow.AddMinutes(-5)  },
            new ServiceRequest { ReferenceNumber = "NOI-CCC", Type = RequestType.NoiseComplaint, FullName = "C", Email = "c@t.com", AddressOrLocation = "Addr", Description = "Desc", CreatedAt = DateTime.UtcNow               }
        );
        await db.SaveChangesAsync();
        var service = new ServiceRequestService(db);

        var result = await service.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("NOI-CCC", result[0].ReferenceNumber);
        Assert.Equal("BIN-BBB", result[1].ReferenceNumber);
        Assert.Equal("POT-AAA", result[2].ReferenceNumber);
    }

    // ── GetByEmail ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_ReturnsOnlyMatchingEmail()
    {
        using var db = DbContextFactory.Create();
        db.ServiceRequests.AddRange(
            new ServiceRequest { ReferenceNumber = "REF-001", Type = RequestType.Other, FullName = "Alice",   Email = "alice@test.com", AddressOrLocation = "Addr", Description = "Desc", CreatedAt = DateTime.UtcNow },
            new ServiceRequest { ReferenceNumber = "REF-002", Type = RequestType.Other, FullName = "Bob",     Email = "bob@test.com",   AddressOrLocation = "Addr", Description = "Desc", CreatedAt = DateTime.UtcNow },
            new ServiceRequest { ReferenceNumber = "REF-003", Type = RequestType.Other, FullName = "Alice 2", Email = "alice@test.com", AddressOrLocation = "Addr", Description = "Desc", CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
        var service = new ServiceRequestService(db);

        var result = await service.GetByEmailAsync("alice@test.com");

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal("alice@test.com", r.Email));
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsEmpty_WhenNoMatch()
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);

        var result = await service.GetByEmailAsync("nobody@test.com");

        Assert.Empty(result);
    }

    // ── UpdateStatus ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(RequestStatus.InReview,   "Under review by our team")]
    [InlineData(RequestStatus.InProgress, "Work scheduled for next week")]
    [InlineData(RequestStatus.Resolved,   "Issue has been fixed")]
    [InlineData(RequestStatus.Closed,     "Closed — duplicate report")]
    public async Task UpdateStatusAsync_UpdatesToAnyValidStatus(
        RequestStatus newStatus, string notes)
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);
        var created = await service.CreateAsync(new CreateServiceRequestDto(
            RequestType.Pothole, "Tom", "tom@test.com", "Road St", "Big pothole"));

        var result = await service.UpdateStatusAsync(created.Id, new UpdateStatusDto(newStatus, notes));

        Assert.NotNull(result);
        Assert.Equal(newStatus, result.Status);
        Assert.Equal(notes, result.AdminNotes);
    }

    [Fact]
    public async Task UpdateStatusAsync_ClearsAdminNotes_WhenNullPassed()
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);
        var created = await service.CreateAsync(new CreateServiceRequestDto(
            RequestType.Other, "Sara", "sara@test.com", "Addr", "Desc"));
        await service.UpdateStatusAsync(created.Id, new UpdateStatusDto(RequestStatus.InReview, "Initial note"));

        var result = await service.UpdateStatusAsync(created.Id, new UpdateStatusDto(RequestStatus.Resolved, null));

        Assert.NotNull(result);
        Assert.Equal(RequestStatus.Resolved, result.Status);
        Assert.Null(result.AdminNotes);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsNull_WhenNotFound()
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);

        var result = await service.UpdateStatusAsync(999, new UpdateStatusDto(RequestStatus.Resolved, null));

        Assert.Null(result);
    }

    // ── New request defaults ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_DefaultsStatus_ToSubmitted()
    {
        using var db = DbContextFactory.Create();
        var service = new ServiceRequestService(db);

        var result = await service.CreateAsync(new CreateServiceRequestDto(
            RequestType.StreetLighting, "Eve", "eve@test.com", "Dark Lane", "Light out"));

        Assert.Equal(RequestStatus.Submitted, result.Status);
    }
}
