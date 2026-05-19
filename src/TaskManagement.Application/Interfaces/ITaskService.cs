using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> GetTasksAsync(TaskFilterDto filter, CancellationToken ct);
    Task<TaskDto?> GetTaskAsync(long id, CancellationToken ct);
    Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, string? idempotencyKey, CancellationToken ct);
    Task<TaskDto> UpdateTaskAsync(long id, UpdateTaskDto dto, CancellationToken ct);
    Task<int> SoftDeleteTasksAsync(IEnumerable<long> ids, CancellationToken ct);
    Task<IReadOnlyList<TaskSummaryRow>> GetSummaryAsync(CancellationToken ct);
}
