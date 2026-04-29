using CivicDesk.API.Controllers;
using CivicDesk.API.DTOs;
using CivicDesk.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CivicDesk.Tests.Controllers;

public class ChatControllerTests
{
    // ── Send ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Send_Returns200_WithChatResponse()
    {
        var mock = new Mock<IChatService>();
        var response = new ChatResponseDto("Hello! How can I help?", null);
        mock.Setup(s => s.SendMessageAsync(It.IsAny<ChatMessageDto>())).ReturnsAsync(response);
        var controller = new ChatController(mock.Object);

        var result = await controller.Send(new ChatMessageDto("session-1", "hey"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task Send_Returns200_WithPreFill_WhenServiceReturnsOne()
    {
        var mock = new Mock<IChatService>();
        var preFill = new PreFillDto("Pothole", "Large pothole on Main St");
        var response = new ChatResponseDto("I'll help you report that pothole.", preFill);
        mock.Setup(s => s.SendMessageAsync(It.IsAny<ChatMessageDto>())).ReturnsAsync(response);
        var controller = new ChatController(mock.Object);

        var result = await controller.Send(new ChatMessageDto("session-1", "There's a pothole on Main St"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var chatResponse = Assert.IsType<ChatResponseDto>(ok.Value);
        Assert.NotNull(chatResponse.PreFill);
        Assert.Equal("Pothole", chatResponse.PreFill.Type);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Send_Returns400_WhenSessionIdIsEmpty()
    {
        var controller = new ChatController(new Mock<IChatService>().Object);

        var result = await controller.Send(new ChatMessageDto("", "hello"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Send_Returns400_WhenMessageIsEmpty()
    {
        var controller = new ChatController(new Mock<IChatService>().Object);

        var result = await controller.Send(new ChatMessageDto("session-1", ""));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Send_Returns400_WhenBothFieldsAreEmpty()
    {
        var controller = new ChatController(new Mock<IChatService>().Object);

        var result = await controller.Send(new ChatMessageDto("", ""));

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
