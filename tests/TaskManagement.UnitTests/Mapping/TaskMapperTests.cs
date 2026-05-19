using FluentAssertions;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Mapping;
using TaskManagement.Domain.Entities;

namespace TaskManagement.UnitTests.Mapping;

public class TaskMapperTests
{
    [Fact]
    public void ToDto_maps_all_fields()
    {
        var now = DateTime.UtcNow;
        var e = new TaskItem
        {
            Id = 7, Title = "T", Description = "D",
            Status = TaskStatus.InProgress, Priority = TaskPriority.High,
            AssignedTo = "carol", CreatedDate = now, ModifiedDate = now,
        };

        var dto = e.ToDto();

        dto.Should().BeEquivalentTo(new TaskDto(7, "T", "D",
            TaskStatus.InProgress, TaskPriority.High, "carol", now, now));
    }

    [Fact]
    public void ToEntity_maps_create_dto()
    {
        var dto = new CreateTaskDto("Title", "Desc", TaskStatus.Done, TaskPriority.Critical, "dan");

        var e = dto.ToEntity();

        e.Title.Should().Be("Title");
        e.Description.Should().Be("Desc");
        e.Status.Should().Be(TaskStatus.Done);
        e.Priority.Should().Be(TaskPriority.Critical);
        e.AssignedTo.Should().Be("dan");
        e.Id.Should().Be(0); // not set by mapper
        e.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Apply_overwrites_mutable_fields_only()
    {
        var e = new TaskItem
        {
            Id = 5, Title = "Old", Description = "Old desc",
            Status = TaskStatus.ToDo, Priority = TaskPriority.Low, AssignedTo = "old",
            IsDeleted = false, IdempotencyKey = "preserve-me",
        };

        new UpdateTaskDto("New", "New desc", TaskStatus.Done, TaskPriority.High, "new").Apply(e);

        e.Id.Should().Be(5);                       // untouched
        e.IdempotencyKey.Should().Be("preserve-me"); // untouched
        e.Title.Should().Be("New");
        e.Description.Should().Be("New desc");
        e.Status.Should().Be(TaskStatus.Done);
        e.Priority.Should().Be(TaskPriority.High);
        e.AssignedTo.Should().Be("new");
    }
}
