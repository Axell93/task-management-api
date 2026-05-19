using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Interfaces;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> ListAsync(TaskStatus? status, TaskPriority? priority, CancellationToken ct);
    Task<TaskItem?> GetByIdAsync(long id, CancellationToken ct);
    Task<TaskItem?> GetByIdempotencyKeyAsync(string key, CancellationToken ct);
    Task AddAsync(TaskItem entity, CancellationToken ct);
    void Update(TaskItem entity);
    Task<int> SoftDeleteAsync(IEnumerable<long> ids, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
}
