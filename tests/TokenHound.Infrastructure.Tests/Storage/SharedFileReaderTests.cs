using AwesomeAssertions;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Tests.Storage;

/// <summary>
/// Verifies concurrent non-locking read operations and graceful missing file handling in <see cref="SharedFileReader"/>.
/// </summary>
public sealed class SharedFileReaderTests
{
    /// <summary>
    /// Verifies that ReadAllTextAsync successfully reads file contents concurrently while an active external writer holds the file open.
    /// </summary>
    [Fact]
    public async Task ReadAllTextAsync_WhenWriterHoldsLockWithSharedAccess_ReadsContentConcurrently()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"shared_file_test_{Guid.NewGuid():N}.txt");

        try
        {
            const string expectedContent = "Concurrently written content from active writer.";

            using var writer = new FileStream(
                tempFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete
            );

            var bytes = Encoding.UTF8.GetBytes(expectedContent);
            writer.Write(bytes);
            writer.Flush();

            var result = await SharedFileReader.ReadAllTextAsync(tempFile, TestContext.Current.CancellationToken);

            result.Should().Be(expectedContent);
        }
        finally
        {

            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Verifies that OpenRead successfully opens a readable stream while an external writer holds the file open.
    /// </summary>
    [Fact]
    public void OpenRead_WhenWriterHoldsLockWithSharedAccess_ReadsStreamConcurrently()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"shared_openread_test_{Guid.NewGuid():N}.txt");

        try
        {
            const string expectedContent = "Concurrently read stream content.";

            using var writer = new FileStream(
                tempFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete
            );

            var bytes = Encoding.UTF8.GetBytes(expectedContent);
            writer.Write(bytes);
            writer.Flush();

            using var readerStream = SharedFileReader.OpenRead(tempFile);
            using var reader = new StreamReader(readerStream, Encoding.UTF8);
            var result = reader.ReadToEnd();

            result.Should().Be(expectedContent);
        }
        finally
        {

            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Verifies that OpenReadStream alias opens a readable stream concurrently without sharing violations.
    /// </summary>
    [Fact]
    public void OpenReadStream_WhenWriterHoldsLockWithSharedAccess_ReadsStreamConcurrently()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"shared_stream_alias_{Guid.NewGuid():N}.txt");

        try
        {
            const string expectedContent = "OpenReadStream alias content.";

            using var writer = new FileStream(
                tempFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete
            );

            var bytes = Encoding.UTF8.GetBytes(expectedContent);
            writer.Write(bytes);
            writer.Flush();

            using var readerStream = SharedFileReader.OpenReadStream(tempFile);
            using var reader = new StreamReader(readerStream, Encoding.UTF8);
            var result = reader.ReadToEnd();

            result.Should().Be(expectedContent);
        }
        finally
        {

            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Verifies that OpenReadAsync returns a valid stream for an existing file.
    /// </summary>
    [Fact]
    public async Task OpenReadAsync_WhenFileExists_ReadsContentSuccessfully()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"shared_async_open_{Guid.NewGuid():N}.txt");

        try
        {
            const string expectedContent = "Asynchronous stream read content.";
            await File.WriteAllTextAsync(tempFile, expectedContent, TestContext.Current.CancellationToken);

            using var stream = await SharedFileReader.OpenReadAsync(tempFile, TestContext.Current.CancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var result = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

            result.Should().Be(expectedContent);
        }
        finally
        {

            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Verifies that ReadAllTextAsync returns null when the target file does not exist.
    /// </summary>
    [Fact]
    public async Task ReadAllTextAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        var nonExistentPath = Path.Combine(
            Path.GetTempPath(),
            $"non_existent_{Guid.NewGuid():N}.txt"
        );

        var result = await SharedFileReader.ReadAllTextAsync(nonExistentPath, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that OpenRead throws FileNotFoundException when the target file does not exist.
    /// </summary>
    [Fact]
    public void OpenRead_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(
            Path.GetTempPath(),
            $"non_existent_{Guid.NewGuid():N}.txt"
        );

        var act = () => SharedFileReader.OpenRead(nonExistentPath);

        act.Should().Throw<FileNotFoundException>();
    }

    /// <summary>
    /// Verifies that OpenReadStream throws FileNotFoundException when the target file does not exist.
    /// </summary>
    [Fact]
    public void OpenReadStream_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(
            Path.GetTempPath(),
            $"non_existent_{Guid.NewGuid():N}.txt"
        );

        var act = () => SharedFileReader.OpenReadStream(nonExistentPath);

        act.Should().Throw<FileNotFoundException>();
    }

    /// <summary>
    /// Verifies that ReadAllTextAsync throws ArgumentException when given null or whitespace paths.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReadAllTextAsync_WhenPathIsNullOrWhitespace_ThrowsArgumentException(string? invalidPath)
    {
        var act = async () => await SharedFileReader.ReadAllTextAsync(invalidPath!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Verifies that OpenRead throws ArgumentException when given null or whitespace paths.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OpenRead_WhenPathIsNullOrWhitespace_ThrowsArgumentException(string? invalidPath)
    {
        var act = () => SharedFileReader.OpenRead(invalidPath!);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that ReadAllTextAsync propagates cancellation tokens properly.
    /// </summary>
    [Fact]
    public async Task ReadAllTextAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"shared_cancel_{Guid.NewGuid():N}.txt");

        try
        {
            await File.WriteAllTextAsync(tempFile, "Cancellation test payload.", TestContext.Current.CancellationToken);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () => await SharedFileReader.ReadAllTextAsync(tempFile, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {

            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Verifies that ReadAllTextAsync respects custom encoding specifications.
    /// </summary>
    [Fact]
    public async Task ReadAllTextAsync_WithCustomEncoding_ReadsMatchingContent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"shared_encoding_{Guid.NewGuid():N}.txt");
        const string expectedText = "Custom encoding payload: 🚀 TokenHound 🐶";

        try
        {
            await File.WriteAllTextAsync(
                tempFile,
                expectedText,
                Encoding.Unicode,
                TestContext.Current.CancellationToken
            );

            var result = await SharedFileReader.ReadAllTextAsync(
                tempFile,
                Encoding.Unicode,
                TestContext.Current.CancellationToken
            );

            result.Should().Be(expectedText);
        }
        finally
        {

            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
