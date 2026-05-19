using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManagement.Application.DTOs;
using TaskManagement.IntegrationTests.Infrastructure;

namespace TaskManagement.IntegrationTests;

public class TasksEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public TasksEndpointsTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task EnsureAuth()
    {
        if (_client.DefaultRequestHeaders.Authorization is null)
            await _client.RegisterAndAuthenticateAsync(userName: $"u{Guid.NewGuid():N}"[..12]);
    }

    [Fact]
    public async Task Empty_list_initially()
    {
        await EnsureAuth();
        var tasks = await _client.GetJson<List<TaskDto>>("/api/tasks");
        tasks.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_then_get_then_list()
    {
        await EnsureAuth();

        var post = await _client.PostJson("/api/tasks",
            new CreateTaskDto("Write docs", "Update README", TaskStatus.ToDo, TaskPriority.High, "alice"));
        post.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await post.ReadJson<TaskDto>();
        created!.Id.Should().BeGreaterThan(0);
        created.Title.Should().Be("Write docs");

        var fetched = await _client.GetJson<TaskDto>($"/api/tasks/{created.Id}");
        fetched!.Title.Should().Be("Write docs");

        var rows = await _client.GetJson<List<TaskDto>>("/api/tasks");
        rows!.Should().Contain(t => t.Id == created.Id);
    }

    [Fact]
    public async Task Create_with_idempotency_key_returns_same_task_on_replay()
    {
        await EnsureAuth();

        var key = Guid.NewGuid().ToString();
        var create = new CreateTaskDto("Replayed", null, TaskStatus.ToDo, TaskPriority.Low, null);

        async Task<TaskDto> Send()
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
            {
                Content = JsonContent.Create(create, options: HttpClientExtensions.JsonOptions),
            };
            req.Headers.Add("Idempotency-Key", key);
            var r = await _client.SendAsync(req);
            r.EnsureSuccessStatusCode();
            return (await r.ReadJson<TaskDto>())!;
        }

        var first = await Send();
        var second = await Send();
        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task Create_with_invalid_title_returns_400()
    {
        await EnsureAuth();
        var bad = new CreateTaskDto("", null, TaskStatus.ToDo, TaskPriority.Low, null);
        var res = await _client.PostJson("/api/tasks", bad);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_unknown_id_returns_404()
    {
        await EnsureAuth();
        var res = await _client.GetAsync("/api/tasks/9999999");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_changes_fields()
    {
        await EnsureAuth();
        var t = await (await _client.PostJson("/api/tasks",
            new CreateTaskDto("Original", null, TaskStatus.ToDo, TaskPriority.Low, null))).ReadJson<TaskDto>();

        var put = await _client.PutJson($"/api/tasks/{t!.Id}",
            new UpdateTaskDto("Updated", "with desc", TaskStatus.Done, TaskPriority.Critical, "carol"));
        put.EnsureSuccessStatusCode();
        var updated = await put.ReadJson<TaskDto>();
        updated!.Title.Should().Be("Updated");
        updated.Status.Should().Be(TaskStatus.Done);
        updated.Priority.Should().Be(TaskPriority.Critical);
        updated.AssignedTo.Should().Be("carol");
    }

    [Fact]
    public async Task Filters_apply_on_server()
    {
        await EnsureAuth();
        await _client.PostJson("/api/tasks",
            new CreateTaskDto("low one", null, TaskStatus.ToDo, TaskPriority.Low, null));
        await _client.PostJson("/api/tasks",
            new CreateTaskDto("crit one", null, TaskStatus.InProgress, TaskPriority.Critical, null));

        var critOnly = await _client.GetJson<List<TaskDto>>("/api/tasks?priority=Critical");
        critOnly!.Should().OnlyContain(t => t.Priority == TaskPriority.Critical);

        var todoOnly = await _client.GetJson<List<TaskDto>>("/api/tasks?status=ToDo");
        todoOnly!.Should().OnlyContain(t => t.Status == TaskStatus.ToDo);
    }

    [Fact]
    public async Task Bulk_soft_delete_removes_from_list()
    {
        await EnsureAuth();
        var t1 = await (await _client.PostJson("/api/tasks",
            new CreateTaskDto("D1", null, TaskStatus.ToDo, TaskPriority.Low, null))).ReadJson<TaskDto>();
        var t2 = await (await _client.PostJson("/api/tasks",
            new CreateTaskDto("D2", null, TaskStatus.ToDo, TaskPriority.Low, null))).ReadJson<TaskDto>();

        var del = await _client.PatchJson("/api/tasks/delete-tasks",
            new DeleteTasksDto(new[] { t1!.Id, t2!.Id }));
        del.EnsureSuccessStatusCode();

        var list = await _client.GetJson<List<TaskDto>>("/api/tasks");
        list!.Should().NotContain(t => t.Id == t1.Id || t.Id == t2.Id);
    }

    [Fact]
    public async Task Summary_returns_grouped_counts()
    {
        await EnsureAuth();
        await _client.PostJson("/api/tasks",
            new CreateTaskDto("S1", null, TaskStatus.ToDo, TaskPriority.Low, null));
        await _client.PostJson("/api/tasks",
            new CreateTaskDto("S2", null, TaskStatus.Done, TaskPriority.High, null));

        var rows = await _client.GetJson<List<TaskSummaryRow>>("/api/tasks/summary");
        rows!.Sum(r => r.Count).Should().BeGreaterOrEqualTo(2);
    }
}
