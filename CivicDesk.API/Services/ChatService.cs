using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using CivicDesk.API.Data;
using CivicDesk.API.DTOs;
using CivicDesk.API.Models;

namespace CivicDesk.API.Services;

public interface IChatService
{
    Task<ChatResponseDto> SendMessageAsync(ChatMessageDto dto);
}

public class ChatService : IChatService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    private const string SystemPrompt = """
        You are CivicAssist, a helpful virtual assistant for a UK council resident self-service portal.
        You help residents report issues, understand council services, and submit service requests.
        
        You can help with: Pothole, MissedBin, NoiseComplaint, PlanningQuery, StreetLighting, Other.
        
        When a resident clearly describes a problem that needs a service request, append this exact block
        at the very end of your response (after your message, on a new line):
        [PREFILL:{"type":"Pothole","description":"Brief description here"}]
        
        Valid type values: Pothole, MissedBin, NoiseComplaint, PlanningQuery, StreetLighting, Other.
        
        Keep responses concise, friendly and professional. Do not discuss topics unrelated to council services.
        Do not include the PREFILL block unless the resident is clearly describing a reportable issue.
        """;

    public ChatService(AppDbContext db, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _db = db;
        _http = httpFactory.CreateClient("gemma");
        _config = config;
    }

    public async Task<ChatResponseDto> SendMessageAsync(ChatMessageDto dto)
    {
        // Persist user message
        _db.ChatMessages.Add(new ChatMessage
        {
            SessionId = dto.SessionId,
            Role = "user",
            Content = dto.Message
        });
        await _db.SaveChangesAsync();

        // Retrieve last 10 messages for context
        var history = await _db.ChatMessages
            .Where(x => x.SessionId == dto.SessionId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        // Build messages array for the model
        var messages = new List<object>
        {
            new { role = "system", content = SystemPrompt }
        };

        foreach (var msg in history)
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        // Call local Gemma endpoint
        var payload = new
        {
            model = _config["Gemma:Model"] ?? "gemma",
            messages,
            temperature = 0.7,
            max_tokens = 512
        };

        var json = JsonSerializer.Serialize(payload);
        var response = await _http.PostAsync(
            "chat/completions",
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(responseJson);
        var rawReply = parsed
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        // Extract prefill block if present
        PreFillDto? preFill = null;
        var reply = rawReply;

        var prefillIndex = rawReply.LastIndexOf("[PREFILL:", StringComparison.Ordinal);
        if (prefillIndex >= 0)
        {
            var prefillRaw = rawReply[prefillIndex..];
            reply = rawReply[..prefillIndex].Trim();

            try
            {
                var jsonStart = prefillRaw.IndexOf('{');
                var jsonEnd = prefillRaw.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var prefillJson = prefillRaw[jsonStart..(jsonEnd + 1)];
                    var prefillParsed = JsonDocument.Parse(prefillJson);
                    var type = prefillParsed.RootElement.GetProperty("type").GetString() ?? string.Empty;
                    var description = prefillParsed.RootElement.GetProperty("description").GetString() ?? string.Empty;
                    preFill = new PreFillDto(type, description);
                }
            }
            catch
            {
                // Malformed prefill — ignore and return reply only
            }
        }

        // Persist assistant reply
        _db.ChatMessages.Add(new ChatMessage
        {
            SessionId = dto.SessionId,
            Role = "assistant",
            Content = reply
        });
        await _db.SaveChangesAsync();

        return new ChatResponseDto(reply, preFill);
    }
}