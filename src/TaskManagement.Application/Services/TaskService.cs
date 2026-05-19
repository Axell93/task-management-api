using FluentValidation;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Mapping;

namespace TaskManagement.Application.Services;

public class TaskService(
    ITaskRepository repo,
    IValidator<CreateTaskDto> createValidator,
    IValidator<UpdateTaskDto> updateValidator,
    IValidator<DeleteTasksDto> deleteValidator,
    ILogger<TaskService> logger) : ITaskService
{
    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(TaskFilterDto filter, CancellationToken ct)
    {
        logger.LogInformation("Listing tasks (status={Status}, priority={Priority})", filter.Status, filter.Priority);
        var items = await repo.ListAsync(filter.Status, filter.Priority, ct);
        logger.LogInformation("Returned {Count} task(s)", items.Count);
        return items.Select(t => t.ToDto()).ToList();
    }

    public async Task<TaskDto?> GetTaskAsync(long id, CancellationToken ct)
    {
        logger.LogInformation("Fetching task {TaskId}", id);
        var item = await repo.GetByIdAsync(id, ct);
        if (item is null) logger.LogInformation("Task {TaskId} not found", id);
        return item?.ToDto();
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, string? idempotencyKey, CancellationToken ct)
    {
        var result = await createValidator.ValidateAsync(dto, ct);
        if (!result.IsValid)
            throw new Common.ValidationException(result.ToDictionary());

        // Idempotency: if the same key was used before, return the prior task
        // without creating a duplicate. The DB also enforces this via a unique
        // filtered index on IdempotencyKey, protecting against race conditions.
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await repo.GetByIdempotencyKeyAsync(idempotencyKey, ct);
            if (existing is not null)
            {
                logger.LogInformation("Idempotency-Key {Key} matched existing task {TaskId}; returning original",
                    idempotencyKey, existing.Id);
                return existing.ToDto();
            }
        }

        var entity = dto.ToEntity();
        entity.IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey;
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Created task {TaskId} (title='{Title}', status={Status}, priority={Priority})",
            entity.Id, entity.Title, entity.Status, entity.Priority);
        return entity.ToDto();
    }

    public async Task<TaskDto> UpdateTaskAsync(long id, UpdateTaskDto dto, CancellationToken ct)
    {
        var result = await updateValidator.ValidateAsync(dto, ct);
        if (!result.IsValid)
            throw new Common.ValidationException(result.ToDictionary());

        var entity = await repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Task {id} not found.");

        // For stricter concurrency, accept the client's RowVersion in the DTO
        // and assign it onto entity.RowVersion before SaveChanges so EF can
        // raise DbUpdateConcurrencyException on conflicting writes.
        dto.Apply(entity);
        repo.Update(entity);
        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Updated task {TaskId}", id);
        return entity.ToDto();
    }

    public async Task<int> SoftDeleteTasksAsync(IEnumerable<long> ids, CancellationToken ct)
    {
        var idList = ids?.ToList() ?? new List<long>();
        var result = await deleteValidator.ValidateAsync(new DeleteTasksDto(idList), ct);
        if (!result.IsValid)
            throw new Common.ValidationException(result.ToDictionary());

        var affected = await repo.SoftDeleteAsync(idList, ct);
        logger.LogInformation("Soft-deleted {Affected} task(s) (requested {Requested})", affected, idList.Count);
        return affected;
    }

    public Task<IReadOnlyList<TaskSummaryRow>> GetSummaryAsync(CancellationToken ct)
    {
        logger.LogInformation("Building task summary");
        return ((ITaskRepository)repo) is ITaskSummaryQuery q
            ? q.GetSummaryAsync(ct)
            : throw new NotSupportedException("Repository does not implement summary query.");
    }
}

// Optional interface: the SQL-backed repository implements this to expose
// the raw-SQL summary while keeping ITaskRepository EF-agnostic.
public interface ITaskSummaryQuery
{
    Task<IReadOnlyList<TaskSummaryRow>> GetSummaryAsync(CancellationToken ct);
}
