using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence;

public class TaskRepository(AppDbContext db, IMemoryCache cache) : ITaskRepository, ITaskSummaryQuery
{
    private const string SummaryCacheKey = "tasks:summary";
    // For a multi-instance deployment, swap IMemoryCache for IDistributedCache
    // (Redis) so all nodes observe the same cached value and eviction.
    public async Task<IReadOnlyList<TaskItem>> ListAsync(TaskStatus? status, TaskPriority? priority, CancellationToken ct)
    {
        var q = db.Tasks.AsNoTracking().AsQueryable();
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (priority.HasValue) q = q.Where(t => t.Priority == priority.Value);
        return await q.OrderByDescending(t => t.CreatedDate).ToListAsync(ct);
    }

    public Task<TaskItem?> GetByIdAsync(long id, CancellationToken ct) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TaskItem?> GetByIdempotencyKeyAsync(string key, CancellationToken ct) =>
        db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.IdempotencyKey == key, ct);

    public async Task AddAsync(TaskItem entity, CancellationToken ct) =>
        await db.Tasks.AddAsync(entity, ct);

    public void Update(TaskItem entity) => db.Tasks.Update(entity);

    public async Task<int> SoftDeleteAsync(IEnumerable<long> ids, CancellationToken ct)
    {
        var idSet = ids.ToHashSet();
        // ExecuteUpdateAsync emits a single UPDATE statement — efficient and
        // avoids loading entities. For concurrency-sensitive deletes, switch
        // to a load-and-update pattern that respects RowVersion.
        var affected = await db.Tasks
            .Where(t => idSet.Contains(t.Id) && !t.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsDeleted, true)
                .SetProperty(t => t.ModifiedDate, DateTime.UtcNow), ct);
        if (affected > 0) cache.Remove(SummaryCacheKey);
        return affected;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        var affected = await db.SaveChangesAsync(ct);
        // Any write to Tasks invalidates the cached summary.
        if (affected > 0) cache.Remove(SummaryCacheKey);
        return affected;
    }

    public async Task<IReadOnlyList<TaskSummaryRow>> GetSummaryAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(SummaryCacheKey, out IReadOnlyList<TaskSummaryRow>? cached) && cached is not null)
            return cached;

        // Raw SQL summary query — intentionally NOT using EF LINQ.
        // CASE expressions translate the int-backed enums to text labels.
        const string sql = @"
SELECT
    CASE [Status]
        WHEN 0 THEN 'ToDo'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'Done'
        ELSE 'Unknown'
    END AS [Status],
    CASE [Priority]
        WHEN 0 THEN 'Low'
        WHEN 1 THEN 'Medium'
        WHEN 2 THEN 'High'
        WHEN 3 THEN 'Critical'
        ELSE 'Unknown'
    END AS [Priority],
    COUNT(*) AS [Count]
FROM [Tasks]
WHERE [IsDeleted] = 0
GROUP BY [Status], [Priority]
ORDER BY [Status], [Priority];";

        var rows = new List<TaskSummaryRow>();
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new TaskSummaryRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2)));
        }

        cache.Set(SummaryCacheKey, (IReadOnlyList<TaskSummaryRow>)rows, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(1),
            Size = 1
        });
        return rows;
    }
}
