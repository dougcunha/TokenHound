using AwesomeAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Infrastructure.Security;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Security;

/// <summary>
/// Verifies Windows Credential Manager interop, cancellation propagation, and argument validation.
/// </summary>
public sealed class WindowsCredentialManagerTests
{
    /// <summary>
    /// Verifies that <see cref="WindowsCredentialManager"/> implements <see cref="ICredentialStore"/>.
    /// </summary>
    [Fact]
    public void WindowsCredentialManager_ImplementsICredentialStore()
    {

        var store = new WindowsCredentialManager();

        store.Should().BeAssignableTo<ICredentialStore>();
    }

    /// <summary>
    /// Verifies that ReadCredentialAsync returns null when querying a non-existent credential target.
    /// </summary>
    [Fact]
    public async Task ReadCredentialAsync_WhenTargetDoesNotExist_ReturnsNull()
    {

        var store = new WindowsCredentialManager();
        var nonExistentTarget = $"tokenhound_test_nonexistent_{Guid.NewGuid():N}";

        var result = await store.ReadCredentialAsync(
            nonExistentTarget,
            TestContext.Current.CancellationToken
        );

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that static ReadCredential returns null when querying a non-existent credential target.
    /// </summary>
    [Fact]
    public void ReadCredential_WhenTargetDoesNotExist_ReturnsNull()
    {

        var nonExistentTarget = $"tokenhound_test_nonexistent_{Guid.NewGuid():N}";

        var result = WindowsCredentialManager.ReadCredential(nonExistentTarget);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ReadCredentialAsync returns a canceled task when cancellation is pre-requested.
    /// </summary>
    [Fact]
    public async Task ReadCredentialAsync_WhenCancellationRequested_ReturnsCanceledTask()
    {

        var store = new WindowsCredentialManager();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var valueTask = store.ReadCredentialAsync("dummy_target", cts.Token);

        valueTask.IsCanceled.Should().BeTrue();

        var act = async () => await valueTask;

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Verifies that ReadCredentialAsync throws ArgumentException when target is null, empty, or whitespace.
    /// </summary>
    /// <param name="invalidTarget">The invalid target argument to test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task ReadCredentialAsync_WhenTargetIsNullOrWhitespace_ThrowsArgumentException(string? invalidTarget)
    {

        var store = new WindowsCredentialManager();

        var act = async () => await store.ReadCredentialAsync(
            invalidTarget!,
            TestContext.Current.CancellationToken
        );

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies that ReadCredential throws ArgumentException when target is null, empty, or whitespace.
    /// </summary>
    /// <param name="invalidTarget">The invalid target argument to test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void ReadCredential_WhenTargetIsNullOrWhitespace_ThrowsArgumentException(string? invalidTarget)
    {

        var act = () => WindowsCredentialManager.ReadCredential(invalidTarget!);

        act.Should().Throw<ArgumentException>();
    }
}
