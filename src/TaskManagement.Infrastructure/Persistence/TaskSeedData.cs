using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence;

/// <summary>
/// HasData seed — runs as part of EF migrations. Produces 60 sample tasks
/// distributed across statuses and priorities so the API has data on first run.
/// </summary>
internal static class TaskSeedData
{
    public static void Apply(ModelBuilder b)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var statuses = Enum.GetValues<TaskStatus>();
        var priorities = Enum.GetValues<TaskPriority>();
        var assignees = new[] { "alice", "bob", "carol", "dave", "erin" };

        var seed = new List<TaskItem>(60);
        for (var i = 1; i <= 60; i++)
        {
            seed.Add(new TaskItem
            {
                Id = i,
                Title = $"Seed task #{i}",
                Description = $"Auto-generated seed task number {i}",
                Status = statuses[i % statuses.Length],
                Priority = priorities[i % priorities.Length],
                AssignedTo = assignees[i % assignees.Length],
                CreatedDate = seedDate.AddMinutes(i),
                ModifiedDate = seedDate.AddMinutes(i),
                IsDeleted = false,
                IdempotencyKey = null
            });
        }

        b.Entity<TaskItem>().HasData(seed);
    }
}
