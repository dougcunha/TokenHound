using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Storage;

/// <summary>
/// Provides non-locking file access primitives for reading files concurrently
/// using <see cref="FileShare.ReadWrite"/> and <see cref="FileShare.Delete"/>.
/// </summary>
public static class SharedFileReader
{
    /// <summary>
    /// Opens a file for reading with non-locking shared read, write, and delete permissions.
    /// </summary>
    /// <param name="path">The full or relative path to the target file.</param>
    /// <returns>A readable <see cref="FileStream"/> configured with non-exclusive sharing.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    public static FileStream OpenRead(string path)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException("The specified file does not exist.", path);

        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete
        );
    }

    /// <summary>
    /// Opens a file stream for reading with non-locking shared read, write, and delete permissions.
    /// Alias for <see cref="OpenRead(string)"/>.
    /// </summary>
    /// <param name="path">The full or relative path to the target file.</param>
    /// <returns>A readable <see cref="FileStream"/> configured with non-exclusive sharing.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    public static FileStream OpenReadStream(string path)
        => OpenRead(path);

    /// <summary>
    /// Asynchronously opens a file for reading with shared read, write, and delete permissions.
    /// </summary>
    /// <param name="path">The full or relative path to the target file.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A readable <see cref="FileStream"/> configured with non-exclusive sharing.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    public static Task<FileStream> OpenReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<FileStream>(cancellationToken);

        return Task.FromResult(OpenRead(path));
    }

    /// <summary>
    /// Asynchronously reads all text from a file with shared read, write, and delete permissions using UTF-8 encoding.
    /// Returns <see langword="null"/> if the file does not exist.
    /// </summary>
    /// <param name="path">The full or relative path to the target file.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The file contents as a string, or <see langword="null"/> if the file does not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    public static Task<string?> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken = default)
        => ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

    /// <summary>
    /// Asynchronously reads all text from a file with shared read, write, and delete permissions using the specified encoding.
    /// Returns <see langword="null"/> if the file does not exist.
    /// </summary>
    /// <param name="path">The full or relative path to the target file.</param>
    /// <param name="encoding">The character encoding to use when reading the file.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The file contents as a string, or <see langword="null"/> if the file does not exist.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is null.</exception>
    public static async Task<string?> ReadAllTextAsync(
        string path,
        Encoding encoding,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(encoding);

        if (!File.Exists(path))
            return null;

        try
        {

            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete
            );

            using var reader = new StreamReader(
                stream,
                encoding,
                detectEncodingFromByteOrderMarks: true
            );

            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {

            return null;
        }
        catch (DirectoryNotFoundException)
        {

            return null;
        }
    }
}
