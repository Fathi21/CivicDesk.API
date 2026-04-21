using Microsoft.AspNetCore.Mvc;
using CivicDesk.API.DTOs;
using CivicDesk.API.Services;

namespace CivicDesk.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _service;

    public ChatController(IChatService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ChatMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SessionId) || string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest("SessionId and Message are required.");

        var result = await _service.SendMessageAsync(dto);
        return Ok(result);
    }
}