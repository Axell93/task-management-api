using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Infrastructure.Auth;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.UnitTests.Auth;

public class IdentityServiceTests
{
    private static Mock<UserManager<ApplicationUser>> NewUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static IOptions<JwtOptions> Jwt() => Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        // Must be ≥ 32 bytes for HmacSha256.
        Key = "0123456789abcdef0123456789abcdef",
        ExpiryMinutes = 30
    });

    private static IdentityService Build(Mock<UserManager<ApplicationUser>> um) =>
        new(um.Object, Jwt(), NullLogger<IdentityService>.Instance);

    [Fact]
    public async Task RegisterAsync_returns_token_when_create_succeeds()
    {
        var um = NewUserManagerMock();
        um.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
          .ReturnsAsync(IdentityResult.Success);

        var result = await Build(um).RegisterAsync(new RegisterDto("alice", "a@x.io", "pw"), default);

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RegisterAsync_throws_validation_when_create_fails()
    {
        var um = NewUserManagerMock();
        var failure = IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Too short." });
        um.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
          .ReturnsAsync(failure);

        var act = () => Build(um).RegisterAsync(new RegisterDto("alice", "a@x.io", "pw"), default);

        var ex = await act.Should().ThrowAsync<Application.Common.ValidationException>();
        ex.Which.Errors.Should().ContainKey("PasswordTooShort");
    }

    [Fact]
    public async Task LoginAsync_throws_unauthorized_when_user_not_found()
    {
        var um = NewUserManagerMock();
        um.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        await Build(um).Invoking(s => s.LoginAsync(new LoginDto("missing", "pw"), default))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task LoginAsync_throws_unauthorized_when_password_wrong()
    {
        var um = NewUserManagerMock();
        var user = new ApplicationUser { Id = "u1", UserName = "alice", Email = "a@x.io" };
        um.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        um.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        um.Setup(m => m.CheckPasswordAsync(user, "bad")).ReturnsAsync(false);
        um.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        await Build(um).Invoking(s => s.LoginAsync(new LoginDto("alice", "bad"), default))
            .Should().ThrowAsync<UnauthorizedException>();

        // Ensure the failed-attempt counter was incremented.
        um.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_throws_locked_when_account_is_locked()
    {
        var um = NewUserManagerMock();
        var user = new ApplicationUser { Id = "u1", UserName = "alice", Email = "a@x.io" };
        um.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        um.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);
        um.Setup(m => m.GetLockoutEndDateAsync(user)).ReturnsAsync(DateTimeOffset.UtcNow.AddMinutes(15));

        await Build(um).Invoking(s => s.LoginAsync(new LoginDto("alice", "anything"), default))
            .Should().ThrowAsync<LockedOutException>();

        // Should NOT even check the password when locked out.
        um.Verify(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_throws_locked_when_failure_pushes_into_lockout()
    {
        var um = NewUserManagerMock();
        var user = new ApplicationUser { Id = "u1", UserName = "alice", Email = "a@x.io" };
        um.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        // First lockout check: false. After AccessFailedAsync, second check: true.
        um.SetupSequence(m => m.IsLockedOutAsync(user))
          .ReturnsAsync(false)
          .ReturnsAsync(true);
        um.Setup(m => m.CheckPasswordAsync(user, "bad")).ReturnsAsync(false);
        um.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        await Build(um).Invoking(s => s.LoginAsync(new LoginDto("alice", "bad"), default))
            .Should().ThrowAsync<LockedOutException>();
    }

    [Fact]
    public async Task LoginAsync_returns_token_and_resets_failure_counter_when_valid()
    {
        var um = NewUserManagerMock();
        var user = new ApplicationUser { Id = "u1", UserName = "alice", Email = "a@x.io" };
        um.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        um.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        um.Setup(m => m.CheckPasswordAsync(user, "good")).ReturnsAsync(true);
        um.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await Build(um).LoginAsync(new LoginDto("alice", "good"), default);

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        um.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }
}
