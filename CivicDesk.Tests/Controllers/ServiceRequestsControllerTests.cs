using CivicDesk.API.Controllers;
using CivicDesk.API.DTOs;
using CivicDesk.API.Models;
using CivicDesk.API.Services;
using Moq;

namespace CivicDesk.Tests.Controllers;

public class ServiceRequestsControllerTests
{
    private static ServiceRequestDto SampleDto(int id = 1, RequestStatus status = RequestStatus.Submitted) => new(
        id, $"POT-REF-00{id}", RequestType.Pothole, status,
        "John Doe", "john@test.com", "Main St", "Pothole on road", null, DateTime.UtcNow);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns201_WithServiceRequestDto()
    {
        var mock = new Mock<IServiceRequestService>();
        var dto = SampleDto();
        mock.Setup(s => s.CreateAsync(It.IsAny<CreateServiceRequestDto>())).ReturnsAsync(dto);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.Create(new CreateServiceRequestDto(
            RequestType.Pothole, "John Doe", "john@test.com", "Main St", "Pothole on road"));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(dto, created.Value);
    }

    [Fact]
    public async Task Create_NewRequest_HasSubmittedStatus()
    {
        var mock = new Mock<IServiceRequestService>();
        var dto = SampleDto(status: RequestStatus.Submitted);
        mock.Setup(s => s.CreateAsync(It.IsAny<CreateServiceRequestDto>())).ReturnsAsync(dto);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.Create(new CreateServiceRequestDto(
            RequestType.MissedBin, "Jane", "jane@test.com", "Addr", "Missed bin"));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var returned = Assert.IsType<ServiceRequestDto>(created.Value);
        Assert.Equal(RequestStatus.Submitted, returned.Status);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200_WithList()
    {
        var mock = new Mock<IServiceRequestService>();
        var list = new List<ServiceRequestDto> { SampleDto(1), SampleDto(2) };
        mock.Setup(s => s.GetAllAsync()).ReturnsAsync(list);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(list, ok.Value);
    }

    [Fact]
    public async Task GetAll_Returns200_WithEmptyList_WhenNoRequests()
    {
        var mock = new Mock<IServiceRequestService>();
        mock.Setup(s => s.GetAllAsync()).ReturnsAsync([]);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<List<ServiceRequestDto>>(ok.Value);
        Assert.Empty(items);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Returns200_WhenFound()
    {
        var mock = new Mock<IServiceRequestService>();
        var dto = SampleDto();
        mock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(dto);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        var mock = new Mock<IServiceRequestService>();
        mock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((ServiceRequestDto?)null);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── GetByReference ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByReference_Returns200_WhenFound()
    {
        var mock = new Mock<IServiceRequestService>();
        var dto = SampleDto();
        mock.Setup(s => s.GetByReferenceAsync("POT-REF-001")).ReturnsAsync(dto);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.GetByReference("POT-REF-001");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetByReference_Returns404_WhenNotFound()
    {
        var mock = new Mock<IServiceRequestService>();
        mock.Setup(s => s.GetByReferenceAsync(It.IsAny<string>())).ReturnsAsync((ServiceRequestDto?)null);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.GetByReference("FAKE-REF");

        Assert.IsType<NotFoundResult>(result);
    }

    // ── UpdateStatus ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(RequestStatus.InReview)]
    [InlineData(RequestStatus.InProgress)]
    [InlineData(RequestStatus.Resolved)]
    [InlineData(RequestStatus.Closed)]
    public async Task UpdateStatus_Returns200_ForAllValidStatuses(RequestStatus status)
    {
        var mock = new Mock<IServiceRequestService>();
        var dto = SampleDto(status: status);
        mock.Setup(s => s.UpdateStatusAsync(1, It.IsAny<UpdateStatusDto>())).ReturnsAsync(dto);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.UpdateStatus(1, new UpdateStatusDto(status, "Admin note"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<ServiceRequestDto>(ok.Value);
        Assert.Equal(status, returned.Status);
    }

    [Fact]
    public async Task UpdateStatus_Returns404_WhenNotFound()
    {
        var mock = new Mock<IServiceRequestService>();
        mock.Setup(s => s.UpdateStatusAsync(999, It.IsAny<UpdateStatusDto>())).ReturnsAsync((ServiceRequestDto?)null);
        var controller = new ServiceRequestsController(mock.Object);

        var result = await controller.UpdateStatus(999, new UpdateStatusDto(RequestStatus.Resolved, null));

        Assert.IsType<NotFoundResult>(result);
    }
}
