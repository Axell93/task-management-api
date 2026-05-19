using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
[EnableRateLimiting("tasks")]
public class TasksController(
    ITaskService service,
    IOutputCacheStore cacheStore,
    ILogger<TasksController> logger) : ControllerBase
{
    /// <summary>GET /api/tasks?status=&priority= — cached 30s per (filter, user).</summary>
    [HttpGet]
    [OutputCache(PolicyName = "TasksList")]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> Get(
        [FromQuery] TaskStatus? status,
        [FromQuery] TaskPriority? priority,
        CancellationToken ct)
    {
        logger.LogInformation("GET /api/tasks (status={Status}, priority={Priority})", status, priority);
        var tasks = await service.GetTasksAsync(new TaskFilterDto(status, priority), ct);
        return Ok(tasks);
    }

    /// <summary>GET /api/tasks/{id}</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<TaskDto>> GetById(long id, CancellationToken ct)
    {
        logger.LogInformation("GET /api/tasks/{TaskId}", id);
        var task = await service.GetTaskAsync(id, ct);
        if (task is null) throw new NotFoundException($"Task {id} not found.");
        return Ok(task);
    }

    /// <summary>
    /// POST /api/tasks — idempotent via the optional 'Idempotency-Key' header.
    /// The same key replayed returns the original task without creating a duplicate.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(
        [FromBody] CreateTaskDto dto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        logger.LogInformation("POST /api/tasks (idempotencyKey={Key})", idempotencyKey ?? "<none>");
        var created = await service.CreateTaskAsync(dto, idempotencyKey, ct);
        await cacheStore.EvictByTagAsync("tasks", ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>PUT /api/tasks/{id}</summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<TaskDto>> Update(long id, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        logger.LogInformation("PUT /api/tasks/{TaskId}", id);
        var updated = await service.UpdateTaskAsync(id, dto, ct);
        await cacheStore.EvictByTagAsync("tasks", ct);
        return Ok(updated);
    }

    /// <summary>PATCH /api/tasks/delete-tasks — soft-delete a batch of ids.</summary>
    [HttpPatch("delete-tasks")]
    public async Task<IActionResult> SoftDelete([FromBody] DeleteTasksDto dto, CancellationToken ct)
    {
        logger.LogInformation("PATCH /api/tasks/delete-tasks (count={Count})", dto.Ids?.Count() ?? 0);
        var affected = await service.SoftDeleteTasksAsync(dto.Ids, ct);
        await cacheStore.EvictByTagAsync("tasks", ct);
        return Ok(new { affected });
    }

    /// <summary>GET /api/tasks/summary — raw-SQL grouped counts. Cached 60s + IMemoryCache inside repo.</summary>
    [HttpGet("summary")]
    [OutputCache(PolicyName = "TasksSummary")]
    public async Task<ActionResult<IReadOnlyList<TaskSummaryRow>>> Summary(CancellationToken ct)
    {
        logger.LogInformation("GET /api/tasks/summary");
        var rows = await service.GetSummaryAsync(ct);
        return Ok(rows);
    }
}
