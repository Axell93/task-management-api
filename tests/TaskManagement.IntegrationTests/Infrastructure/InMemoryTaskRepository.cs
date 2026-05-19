using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Drop-in repository for integration tests. Mirrors the production
/// TaskRepository but expresses the summary via LINQ so it works on the
/// EF InMemory provider (which cannot execute raw SQL).
/// </summary>
public class InMemoryTaskRepository(AppDbContext db) : ITaskRepository, ITaskSummaryQuery
{
    public async Task<IReadOnlyList<TaskItem>> ListAsync(TaskStatus? status, TaskPriority? priority, CancellationToken ct)
    {
        var q = db.Tasks.AsQueryable();
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (priority.HasValue) q = q.Where(t => t.Priority == priority.Value);
        return await q.OrderByDescending(t => t.CreatedDate).ToListAsync(ct);
    }

    public Task<TaskItem?> GetByIdAsync(long id, CancellationToken ct) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TaskItem?> GetByIdempotencyKeyAsync(string key, CancellationToken ct) =>
        db.Tasks.FirstOrDefaultAsync(t => t.IdempotencyKey == key, ct);

    public async Task AddAsync(TaskItem entity, CancellationToken ct) =>
        await db.Tasks.AddAsync(entity, ct);

    public void Update(TaskItem entity) => db.Tasks.Update(entity);

    public async Task<int> SoftDeleteAsync(IEnumerable<long> ids, CancellationToken ct)
    {
        var idSet = ids.ToHashSet();
        var rows = await db.Tasks.Where(t => idSet.Contains(t.Id) && !t.IsDeleted).ToListAsync(ct);
        foreach (var r in rows)
        {
            r.IsDeleted = true;
            r.ModifiedDate = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<TaskSummaryRow>> GetSummaryAsync(CancellationToken ct)
    {
        var rows = await db.Tasks
            .Where(t => !t.IsDeleted)
            .GroupBy(t => new { t.Status, t.Priority })
            .Select(g => new TaskSummaryRow(g.Key.Status.ToString(), g.Key.Priority.ToString(), g.Count()))
            .ToListAsync(ct);
        return rows;
    }
}
