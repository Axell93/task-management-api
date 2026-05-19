using FluentAssertions;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Validators;

namespace TaskManagement.UnitTests.Validators;

public class CreateTaskDtoValidatorTests
{
    private readonly CreateTaskDtoValidator _sut = new();

    [Fact]
    public void Valid_dto_passes()
    {
        var r = _sut.Validate(new CreateTaskDto("Title", "Desc", TaskStatus.ToDo, TaskPriority.Low, "alice"));
        r.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Empty_title_fails(string? title)
    {
        var r = _sut.Validate(new CreateTaskDto(title!, null, TaskStatus.ToDo, TaskPriority.Low, null));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Title_longer_than_200_fails()
    {
        var r = _sut.Validate(new CreateTaskDto(new string('x', 201), null, TaskStatus.ToDo, TaskPriority.Low, null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Invalid_status_enum_fails()
    {
        var r = _sut.Validate(new CreateTaskDto("T", null, (TaskStatus)42, TaskPriority.Low, null));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Fact]
    public void Invalid_priority_enum_fails()
    {
        var r = _sut.Validate(new CreateTaskDto("T", null, TaskStatus.ToDo, (TaskPriority)99, null));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "Priority");
    }
}

public class UpdateTaskDtoValidatorTests
{
    private readonly UpdateTaskDtoValidator _sut = new();

    [Fact]
    public void Valid_dto_passes()
    {
        var r = _sut.Validate(new UpdateTaskDto("Title", "Desc", TaskStatus.Done, TaskPriority.High, "bob"));
        r.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_title_fails()
    {
        var r = _sut.Validate(new UpdateTaskDto("", null, TaskStatus.ToDo, TaskPriority.Low, null));
        r.IsValid.Should().BeFalse();
    }
}

public class DeleteTasksDtoValidatorTests
{
    private readonly DeleteTasksDtoValidator _sut = new();

    [Fact]
    public void Empty_ids_fail()
    {
        var r = _sut.Validate(new DeleteTasksDto(Array.Empty<long>()));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Non_empty_ids_pass()
    {
        var r = _sut.Validate(new DeleteTasksDto(new long[] { 1, 2 }));
        r.IsValid.Should().BeTrue();
    }
}

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _sut = new();

    [Fact]
    public void Valid_register_passes()
    {
        _sut.Validate(new RegisterDto("alice", "alice@x.io", "secret123"))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("alice", "not-an-email", "secret123")]
    [InlineData("", "alice@x.io", "secret123")]
    [InlineData("alice", "alice@x.io", "12")]
    public void Bad_inputs_fail(string u, string e, string p)
    {
        _sut.Validate(new RegisterDto(u, e, p)).IsValid.Should().BeFalse();
    }
}

public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _sut = new();

    [Fact]
    public void Both_fields_required()
    {
        _sut.Validate(new LoginDto("", "")).IsValid.Should().BeFalse();
        _sut.Validate(new LoginDto("alice", "pw")).IsValid.Should().BeTrue();
    }
}
