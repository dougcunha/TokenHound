using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace TokenHound.Core.Models;

/// <summary>
/// A release version in <c>MAJOR.MINOR.PATCH[-prerelease]</c> form, parsed from a Git tag or an assembly informational version.
/// </summary>
public sealed record ReleaseVersion : IComparable<ReleaseVersion>
{
    private const char METADATA_SEPARATOR = '+';
    private const char PRERELEASE_SEPARATOR = '-';
    private const char PART_SEPARATOR = '.';

    /// <summary>
    /// Gets the major version number.
    /// </summary>
    public required int Major { get; init; }

    /// <summary>
    /// Gets the minor version number.
    /// </summary>
    public required int Minor { get; init; }

    /// <summary>
    /// Gets the patch version number.
    /// </summary>
    public required int Patch { get; init; }

    /// <summary>
    /// Gets the prerelease label (for example <c>beta.1</c>), or <see langword="null"/> for a stable release.
    /// </summary>
    public string? Prerelease { get; init; }

    /// <summary>
    /// Gets a value indicating whether this version carries a prerelease label.
    /// </summary>
    public bool IsPrerelease
        => Prerelease is not null;

    /// <summary>
    /// Parses a version such as <c>v1.2.3</c>, <c>1.2.3-beta.1</c>, or <c>1.2.3+abc123</c>; build metadata is discarded.
    /// </summary>
    /// <param name="text">The tag or informational version text.</param>
    /// <param name="version">The parsed version when the text is valid.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is a valid version; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? text, [NotNullWhen(true)] out ReleaseVersion? version)
    {

        version = null;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = StripMetadata(text.Trim());

        if (value.StartsWith('v') || value.StartsWith('V'))
            value = value[1..];

        var separatorIndex = value.IndexOf(PRERELEASE_SEPARATOR);
        var core = separatorIndex < 0 ? value : value[..separatorIndex];
        var prerelease = separatorIndex < 0 ? null : value[(separatorIndex + 1)..];

        if (prerelease is not null && !IsValidPrerelease(prerelease))
            return false;

        version = CreateVersion(core, prerelease);

        return version is not null;
    }

    /// <inheritdoc />
    public int CompareTo(ReleaseVersion? other)
    {

        if (other is null)
            return 1;

        var coreComparison = CompareCore(other);

        if (coreComparison != 0)
            return coreComparison;

        return ComparePrerelease(Prerelease, other.Prerelease);
    }

    /// <summary>
    /// Determines whether this version sorts after <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The version to compare against.</param>
    /// <returns><see langword="true"/> when this version is newer; otherwise, <see langword="false"/>.</returns>
    public bool IsNewerThan(ReleaseVersion other)
        => CompareTo(other) > 0;

    /// <inheritdoc />
    public override string ToString()
        => Prerelease is null
            ? string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}")
            : string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}-{Prerelease}");

    private static string StripMetadata(string value)
    {

        var metadataIndex = value.IndexOf(METADATA_SEPARATOR);

        return metadataIndex < 0 ? value : value[..metadataIndex];
    }

    private static ReleaseVersion? CreateVersion(string core, string? prerelease)
    {

        var parts = core.Split(PART_SEPARATOR);

        if (parts.Length != 3
            || !TryParseNumber(parts[0], out var major)
            || !TryParseNumber(parts[1], out var minor)
            || !TryParseNumber(parts[2], out var patch))
            return null;

        return new ReleaseVersion { Major = major, Minor = minor, Patch = patch, Prerelease = prerelease };
    }

    private static bool TryParseNumber(string part, out int number)
        => int.TryParse(
            part,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out number
        );

    private static bool IsValidPrerelease(string prerelease)
    {

        if (prerelease.Length == 0)
            return false;

        foreach (var identifier in prerelease.Split(PART_SEPARATOR))
        {
            if (identifier.Length == 0 || !identifier.All(static c => char.IsAsciiLetterOrDigit(c) || c == PRERELEASE_SEPARATOR))
                return false;
        }

        return true;
    }

    private int CompareCore(ReleaseVersion other)
    {

        var major = Major.CompareTo(other.Major);

        if (major != 0)
            return major;

        var minor = Minor.CompareTo(other.Minor);

        return minor != 0 ? minor : Patch.CompareTo(other.Patch);
    }

    private static int ComparePrerelease(string? left, string? right)
    {

        if (left is null || right is null)
            return left is null ? (right is null ? 0 : 1) : -1;

        var leftParts = left.Split(PART_SEPARATOR);
        var rightParts = right.Split(PART_SEPARATOR);
        var shared = Math.Min(leftParts.Length, rightParts.Length);

        for (var index = 0; index < shared; index++)
        {
            var comparison = CompareIdentifier(leftParts[index], rightParts[index]);

            if (comparison != 0)
                return comparison;
        }

        return leftParts.Length.CompareTo(rightParts.Length);
    }

    private static int CompareIdentifier(string left, string right)
    {

        var leftIsNumber = TryParseNumber(left, out var leftNumber);
        var rightIsNumber = TryParseNumber(right, out var rightNumber);

        return (leftIsNumber, rightIsNumber) switch
        {
            (true, true) => leftNumber.CompareTo(rightNumber),
            (true, false) => -1,
            (false, true) => 1,
            _ => string.CompareOrdinal(left, right)
        };
    }
}
