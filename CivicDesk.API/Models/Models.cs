using System;

namespace CivicDesk.API.Models
{
    // ENUMS
    public enum RequestType
    {
        General,
        Technical,
        Complaint,
        Other
    }

    public enum RequestStatus
    {
        Pending,
        InProgress,
        Resolved,
        Rejected
    }

    // ENTITIES
    public class ServiceRequest
    {
        public int Id { get; set; } // PK

        public string ReferenceNumber { get; set; }

        public RequestType Type { get; set; }

        public RequestStatus Status { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string AddressOrLocation { get; set; }

        public string Description { get; set; }

        public string? AdminNotes { get; set; } // nullable

        public DateTime CreatedAt { get; set; }
    }

    public class ChatMessage
    {
        public int Id { get; set; } // PK

        public string SessionId { get; set; }

        public string Role { get; set; } // "user" | "assistant"

        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}