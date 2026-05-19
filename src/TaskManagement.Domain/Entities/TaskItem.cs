namespace TaskManagement.Domain.Entities;

public class TaskItem
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public string? AssignedTo { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }

    // Idempotency key (nullable, unique-filtered index). Clients supplying
    // an Idempotency-Key header allow safe POST retries.
    public string? IdempotencyKey { get; set; }

    // Concurrency token. SQL Server rowversion auto-updates on each write;
    // EF compares it on UPDATE/DELETE to detect lost updates.
    // To enhance further: catch DbUpdateConcurrencyException in the service layer
    // and apply a domain-driven merge / retry strategy.
    public byte[]? RowVersion { get; set; }
}
