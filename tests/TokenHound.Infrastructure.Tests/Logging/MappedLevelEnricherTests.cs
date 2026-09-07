using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Serilog.Events;
using Serilog.Parsing;
using TokenHound.Infrastructure.Logging;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Logging;

/// <summary>
/// Verifies log event level mapping and source context enrichment.
/// </summary>
public sealed class MappedLevelEnricherTests
{
    private static readonly MessageTemplateParser PARSER = new();

    /// <summary>
    /// Validates mapped level property injection.
    /// </summary>
    [Theory]
    [InlineData(LogEventLevel.Information, "INFO")]
    [InlineData(LogEventLevel.Debug, "DEBUG")]
    [InlineData(LogEventLevel.Warning, "WARNING")]
    [InlineData(LogEventLevel.Error, "ERROR")]
    public void Enrich_AddsMappedLevelProperty(LogEventLevel level, string expectedMapped)
    {

        var mapping = new Dictionary<string, string>
        {
            ["Information"] = "INFO",
            ["Debug"] = "DEBUG",
            ["Warning"] = "WARNING",
            ["Error"] = "ERROR"
        };
        var enricher = new MappedLevelEnricher(mapping, "TokenHound");
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            null,
            PARSER.Parse("Test message"),
            []
        );

        enricher.Enrich(logEvent, new PropertyFactoryStub());

        logEvent.Properties.Should().ContainKey("MappedLevel");
        logEvent.Properties["MappedLevel"].ToString().Trim('"').Should().Be(expectedMapped);
        logEvent.Properties.Should().ContainKey("SourceContext");
        logEvent.Properties["SourceContext"].ToString().Trim('"').Should().Be("TokenHound");
    }

    private sealed class PropertyFactoryStub : Serilog.Core.ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            => new(name, new ScalarValue(value));
    }
}
