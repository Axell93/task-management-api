using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Mapping;

public static class TaskMapper
{
    public static TaskDto ToDto(this TaskItem e) => new(
        e.Id, e.Title, e.Description, e.Status, e.Priority,
        e.AssignedTo, e.CreatedDate, e.ModifiedDate);

    public static TaskItem ToEntity(this CreateTaskDto dto) => new()
    {
        Title = dto.Title,
        Description = dto.Description,
        Status = dto.Status,
        Priority = dto.Priority,
        AssignedTo = dto.AssignedTo
    };

    public static void Apply(this UpdateTaskDto dto, TaskItem e)
    {
        e.Title = dto.Title;
        e.Description = dto.Description;
        e.Status = dto.Status;
        e.Priority = dto.Priority;
        e.AssignedTo = dto.AssignedTo;
    }
}
