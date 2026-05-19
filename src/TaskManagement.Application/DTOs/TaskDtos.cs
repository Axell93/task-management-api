using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs;

public record CreateTaskDto(
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    string? AssignedTo);

public record UpdateTaskDto(
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    string? AssignedTo);

public record TaskDto(
    long Id,
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    string? AssignedTo,
    DateTime CreatedDate,
    DateTime ModifiedDate);

public record TaskFilterDto(TaskStatus? Status, TaskPriority? Priority);

public record DeleteTasksDto(IEnumerable<long> Ids);

public record TaskSummaryRow(string Status, string Priority, int Count);
