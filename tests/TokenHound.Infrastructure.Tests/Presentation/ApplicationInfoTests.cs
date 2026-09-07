using AwesomeAssertions;
using System;
using System.Reflection;
using System.Reflection.Emit;
using TokenHound.App.Presentation;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Presentation;

/// <summary>
/// Unit tests verifying <see cref="ApplicationInfo"/> metadata extraction, assembly injection, and version fallbacks.
/// </summary>
public sealed class ApplicationInfoTests
{
    /// <summary>
    /// Verifies that an explicit assembly with informational version retains prerelease and build metadata.
    /// </summary>
    [Fact]
    public void FromAssembly_WithInformationalVersion_PreservesFullPrereleaseAndBuildMetadata()
    {

        var versionString = "1.2.3-preview.4+git.abcdef1234567890";
        var assembly = CreateDynamicAssembly(informationalVersion: versionString);

        var info = ApplicationInfo.FromAssembly(assembly);

        info.DisplayVersion.Should().Be(versionString);
        info.Version.Should().Be(versionString);
        info.Name.Should().Be(ApplicationInfo.DEFAULT_NAME);
        info.Description.Should().Be(ApplicationInfo.DEFAULT_DESCRIPTION);
    }

    /// <summary>
    /// Verifies that an explicit assembly with whitespace informational version falls back to AssemblyVersion.
    /// </summary>
    [Fact]
    public void FromAssembly_WithWhitespaceInformationalVersion_FallsBackToAssemblyVersion()
    {

        var expectedVersion = new Version(2, 3, 4, 5);
        var assembly = CreateDynamicAssembly(
            informationalVersion: "   ",
            assemblyVersion: expectedVersion);

        var info = ApplicationInfo.FromAssembly(assembly);

        info.DisplayVersion.Should().Be("2.3.4.5");
        info.Version.Should().Be("2.3.4.5");
    }

    /// <summary>
    /// Verifies that an explicit assembly with no informational version attribute falls back to AssemblyVersion.
    /// </summary>
    [Fact]
    public void FromAssembly_WithNoInformationalVersionAttribute_FallsBackToAssemblyVersion()
    {

        var expectedVersion = new Version(1, 0, 0, 0);
        var assembly = CreateDynamicAssembly(
            informationalVersion: null,
            assemblyVersion: expectedVersion);

        var info = ApplicationInfo.FromAssembly(assembly);

        info.DisplayVersion.Should().Be("1.0.0.0");
        info.Version.Should().Be("1.0.0.0");
    }

    /// <summary>
    /// Verifies that an explicit assembly with no attribute and null AssemblyVersion returns unavailable text.
    /// </summary>
    [Fact]
    public void FromAssembly_WithNoInformationalVersionAndNullAssemblyVersion_ReturnsUnavailable()
    {

        var assembly = CreateDynamicAssembly(
            informationalVersion: null,
            assemblyVersion: null);

        var info = ApplicationInfo.FromAssembly(assembly);

        info.DisplayVersion.Should().Be(ApplicationInfo.UNAVAILABLE_VERSION);
        info.Version.Should().Be(ApplicationInfo.UNAVAILABLE_VERSION);
    }

    /// <summary>
    /// Verifies that leading and trailing whitespace in informational version is trimmed.
    /// </summary>
    [Fact]
    public void FromAssembly_WithWhitespacePadding_TrimsDisplayVersion()
    {

        var assembly = CreateDynamicAssembly(informationalVersion: "  3.4.5-beta.1  ");

        var info = ApplicationInfo.FromAssembly(assembly);

        info.DisplayVersion.Should().Be("3.4.5-beta.1");
    }

    /// <summary>
    /// Verifies that long build version strings with commits and branch names are preserved without truncation.
    /// </summary>
    [Fact]
    public void FromAssembly_WithLongVersionMetadata_PreservesFullString()
    {

        var longVersion = "10.0.0-preview.2.24521.1+build.sha.1234567890abcdef1234567890abcdef12345678-branch-feature-context-menu-long-tag";
        var assembly = CreateDynamicAssembly(informationalVersion: longVersion);

        var info = ApplicationInfo.FromAssembly(assembly);

        info.DisplayVersion.Should().Be(longVersion);
    }

    /// <summary>
    /// Verifies that the fixed product name and description are always returned.
    /// </summary>
    [Fact]
    public void FromAssembly_PreservesFixedProductNameAndDescription()
    {

        var assembly = CreateDynamicAssembly(informationalVersion: "0.1.0");

        var info = ApplicationInfo.FromAssembly(assembly);

        info.Name.Should().Be("TokenHound");
        info.Description.Should().Be("TokenHound monitors LLM usage, rate limits, and agent activity across AI coding tools on your Windows desktop.");
    }

    /// <summary>
    /// Verifies that ApplicationInfo.Current returns non-null application metadata.
    /// </summary>
    [Fact]
    public void Current_ReturnsValidApplicationInfo()
    {

        var info = ApplicationInfo.Current;

        info.Should().NotBeNull();
        info.Name.Should().Be("TokenHound");
        info.Description.Should().NotBeNullOrWhiteSpace();
        info.DisplayVersion.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Verifies that Get delegates to FromAssembly.
    /// </summary>
    [Fact]
    public void Get_WithExplicitAssembly_DelegatesToFromAssembly()
    {

        var versionString = "9.9.9-test";
        var assembly = CreateDynamicAssembly(informationalVersion: versionString);

        var fromAssembly = ApplicationInfo.FromAssembly(assembly);
        var fromGet = ApplicationInfo.Get(assembly);

        fromGet.Should().Be(fromAssembly);
    }

    /// <summary>
    /// Verifies that FromAssembly with null argument falls back to the current application assembly.
    /// </summary>
    [Fact]
    public void FromAssembly_WithNullAssembly_FallsBackToCurrent()
    {

        var fromNull = ApplicationInfo.FromAssembly(null);
        var current = ApplicationInfo.Current;

        fromNull.Name.Should().Be(current.Name);
        fromNull.Description.Should().Be(current.Description);
        fromNull.DisplayVersion.Should().Be(current.DisplayVersion);
    }

    private static Assembly CreateDynamicAssembly(
        string? informationalVersion = null,
        Version? assemblyVersion = null)
    {

        var name = new AssemblyName($"DynamicTestAssembly_{Guid.NewGuid():N}")
        {
            Version = assemblyVersion
        };

        var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

        if (informationalVersion is not null)
        {
            var ctor = typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!;
            var attrBuilder = new CustomAttributeBuilder(ctor, [informationalVersion]);

            builder.SetCustomAttribute(attrBuilder);
        }

        return builder;
    }
}
