using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Application.Validators;
using TaskManagement.Domain.Entities;

namespace TaskManagement.UnitTests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _repo = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(
            _repo.Object,
            new CreateTaskDtoValidator(),
            new UpdateTaskDtoValidator(),
            new DeleteTasksDtoValidator(),
            NullLogger<TaskService>.Instance);
    }

    [Fact]
    public async Task GetTasksAsync_returns_mapped_dtos()
    {
        _repo.Setup(r => r.ListAsync(null, null, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<TaskItem>
             {
                 new() { Id = 1, Title = "A", Status = TaskStatus.ToDo, Priority = TaskPriority.Low }
             });

        var result = await _sut.GetTasksAsync(new TaskFilterDto(null, null), default);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("A");
    }

    [Fact]
    public async Task GetTaskAsync_returns_null_when_not_found()
    {
        _repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
             .ReturnsAsync((TaskItem?)null);

        var result = await _sut.GetTaskAsync(99, default);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateTaskAsync_throws_validation_when_title_missing()
    {
        var dto = new CreateTaskDto("", null, TaskStatus.ToDo, TaskPriority.Medium, null);

        await FluentActions.Invoking(() => _sut.CreateTaskAsync(dto, null, default))
            .Should().ThrowAsync<Application.Common.ValidationException>();
    }

    [Fact]
    public async Task CreateTaskAsync_throws_validation_when_status_invalid()
    {
        var dto = new CreateTaskDto("Valid", null, (TaskStatus)999, TaskPriority.Medium, null);

        await FluentActions.Invoking(() => _sut.CreateTaskAsync(dto, null, default))
            .Should().ThrowAsync<Application.Common.ValidationException>();
    }

    [Fact]
    public async Task CreateTaskAsync_persists_and_returns_dto()
    {
        var dto = new CreateTaskDto("New", "desc", TaskStatus.ToDo, TaskPriority.High, "alice");

        _repo.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
             .Callback<TaskItem, CancellationToken>((t, _) => t.Id = 42)
             .Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateTaskAsync(dto, null, default);

        result.Id.Should().Be(42);
        result.Title.Should().Be("New");
        _repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_returns_existing_when_idempotency_key_matches()
    {
        var dto = new CreateTaskDto("New", null, TaskStatus.ToDo, TaskPriority.Low, null);
        var existing = new TaskItem { Id = 7, Title = "Existing", IdempotencyKey = "key-1" };

        _repo.Setup(r => r.GetByIdempotencyKeyAsync("key-1", It.IsAny<CancellationToken>()))
             .ReturnsAsync(existing);

        var result = await _sut.CreateTaskAsync(dto, "key-1", default);

        result.Id.Should().Be(7);
        result.Title.Should().Be("Existing");
        _repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskAsync_throws_not_found_when_missing()
    {
        var dto = new UpdateTaskDto("T", null, TaskStatus.ToDo, TaskPriority.Low, null);
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync((TaskItem?)null);

        await FluentActions.Invoking(() => _sut.UpdateTaskAsync(1, dto, default))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateTaskAsync_updates_fields_and_saves()
    {
        var entity = new TaskItem { Id = 5, Title = "Old", Status = TaskStatus.ToDo };
        _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dto = new UpdateTaskDto("New", "d", TaskStatus.Done, TaskPriority.Critical, "bob");

        var result = await _sut.UpdateTaskAsync(5, dto, default);

        result.Title.Should().Be("New");
        result.Status.Should().Be(TaskStatus.Done);
        result.Priority.Should().Be(TaskPriority.Critical);
        _repo.Verify(r => r.Update(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteTasksAsync_throws_when_ids_empty()
    {
        await FluentActions.Invoking(() => _sut.SoftDeleteTasksAsync(Array.Empty<long>(), default))
            .Should().ThrowAsync<Application.Common.ValidationException>();
    }

    [Fact]
    public async Task SoftDeleteTasksAsync_returns_affected_row_count()
    {
        _repo.Setup(r => r.SoftDeleteAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(3);

        var result = await _sut.SoftDeleteTasksAsync(new long[] { 1, 2, 3 }, default);

        result.Should().Be(3);
    }

    [Fact]
    public async Task GetSummaryAsync_delegates_to_repository()
    {
        var repo = new Mock<ITaskRepository>();
        var summary = repo.As<ITaskSummaryQuery>();
        summary.Setup(s => s.GetSummaryAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<TaskSummaryRow> { new("ToDo", "Low", 5) });

        var sut = new TaskService(repo.Object,
            new CreateTaskDtoValidator(),
            new UpdateTaskDtoValidator(),
            new DeleteTasksDtoValidator(),
            NullLogger<TaskService>.Instance);

        var result = await sut.GetSummaryAsync(default);
        result.Should().ContainSingle().Which.Count.Should().Be(5);
    }
}
