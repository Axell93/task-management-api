using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> b)
    {
        b.ToTable("Tasks");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.AssignedTo).HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Priority).HasConversion<int>();

        // Concurrency token (SQL Server rowversion). EF includes this in
        // UPDATE/DELETE WHERE clauses; a 0-row result throws
        // DbUpdateConcurrencyException — extend handling here.
        b.Property(x => x.RowVersion).IsRowVersion();

        b.Property(x => x.IdempotencyKey).HasMaxLength(128);

        // Soft-delete filter: every query excludes deleted rows by default.
        // Use IgnoreQueryFilters() in admin/reporting code paths if needed.
        b.HasQueryFilter(x => !x.IsDeleted);

        // Indexes:
        //   - Unique filtered index enforces POST idempotency at the DB.
        //   - Status / Priority indexes accelerate the common filter combos.
        //   - AssignedTo index supports filtering by assignee.
        b.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_Tasks_IdempotencyKey");

        b.HasIndex(x => x.Status).HasDatabaseName("IX_Tasks_Status");
        b.HasIndex(x => x.Priority).HasDatabaseName("IX_Tasks_Priority");
        b.HasIndex(x => new { x.Status, x.Priority }).HasDatabaseName("IX_Tasks_Status_Priority");
        b.HasIndex(x => x.AssignedTo).HasDatabaseName("IX_Tasks_AssignedTo");
        b.HasIndex(x => x.IsDeleted).HasDatabaseName("IX_Tasks_IsDeleted");
    }
}
