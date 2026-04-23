using CivicDesk.API.Models;

namespace CivicDesk.API.DTOs;

public record CreateServiceRequestDto(
    RequestType Type,
    string FullName,
    string Email,
    string AddressOrLocation,
    string Description
);

public record ServiceRequestDto(
    int Id,
    string ReferenceNumber,
    RequestType Type,
    RequestStatus Status,
    string FullName,
    string Email,
    string AddressOrLocation,
    string Description,
    string? AdminNotes,
    DateTime CreatedAt
);

public record UpdateStatusDto(
    RequestStatus Status,
    string? AdminNotes
);

public record LoginDto(string Username, string Password);

public record ResidentLoginDto(string Email, string ReferenceNumber);

public record AuthTokenDto(string Token, DateTime ExpiresAt);

public record ChatMessageDto(
    string SessionId,
    string Message
);

public record PreFillDto(
    string Type,
    string Description
);

public record ChatResponseDto(
    string Reply,
    PreFillDto? PreFill
);