using Microsoft.EntityFrameworkCore;
using CivicDesk.API.Data;
using CivicDesk.API.DTOs;
using CivicDesk.API.Models;

namespace CivicDesk.API.Services;

public interface IServiceRequestService
{
    Task<ServiceRequestDto> CreateAsync(CreateServiceRequestDto dto);
    Task<ServiceRequestDto?> GetByIdAsync(int id);
    Task<ServiceRequestDto?> GetByReferenceAsync(string reference);
    Task<List<ServiceRequestDto>> GetAllAsync();
    Task<ServiceRequestDto?> UpdateStatusAsync(int id, UpdateStatusDto dto);
}

public class ServiceRequestService : IServiceRequestService
{
    private readonly AppDbContext _db;

    public ServiceRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceRequestDto> CreateAsync(CreateServiceRequestDto dto)
    {
        var request = new ServiceRequest
        {
            Type = dto.Type,
            FullName = dto.FullName,
            Email = dto.Email,
            AddressOrLocation = dto.AddressOrLocation,
            Description = dto.Description,
            ReferenceNumber = GenerateReference(dto.Type)
        };

        _db.ServiceRequests.Add(request);
        await _db.SaveChangesAsync();

        return ToDto(request);
    }

    public async Task<ServiceRequestDto?> GetByIdAsync(int id)
    {
        var request = await _db.ServiceRequests.FindAsync(id);
        return request is null ? null : ToDto(request);
    }

    public async Task<ServiceRequestDto?> GetByReferenceAsync(string reference)
    {
        var request = await _db.ServiceRequests
            .FirstOrDefaultAsync(x => x.ReferenceNumber == reference);
        return request is null ? null : ToDto(request);
    }

    public async Task<List<ServiceRequestDto>> GetAllAsync()
    {
        var requests = await _db.ServiceRequests
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return requests.Select(ToDto).ToList();
    }

    public async Task<ServiceRequestDto?> UpdateStatusAsync(int id, UpdateStatusDto dto)
    {
        var request = await _db.ServiceRequests.FindAsync(id);
        if (request is null) return null;

        request.Status = dto.Status;
        request.AdminNotes = dto.AdminNotes;
        await _db.SaveChangesAsync();

        return ToDto(request);
    }

    private static string GenerateReference(RequestType type)
    {
        var prefix = type switch
        {
            RequestType.Pothole => "POT",
            RequestType.MissedBin => "BIN",
            RequestType.NoiseComplaint => "NOI",
            RequestType.PlanningQuery => "PLN",
            RequestType.StreetLighting => "STL",
            RequestType.Other => "OTH",
            _ => "OTH"
        };

        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpper();

        return $"{prefix}-{date}-{suffix}";
    }

    private static ServiceRequestDto ToDto(ServiceRequest r) => new(
        r.Id,
        r.ReferenceNumber,
        r.Type,
        r.Status,
        r.FullName,
        r.Email,
        r.AddressOrLocation,
        r.Description,
        r.AdminNotes,
        r.CreatedAt
    );
}