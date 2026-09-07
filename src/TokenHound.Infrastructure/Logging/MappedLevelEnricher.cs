using System;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace TokenHound.Infrastructure.Logging;

/// <summary>
/// Enriches Serilog log events with a mapped log level name and fallback source context.
/// </summary>
public sealed class MappedLevelEnricher : ILogEventEnricher
{
    private const string MAPPED_LEVEL_PROPERTY = "MappedLevel";
    private const string SOURCE_CONTEXT_PROPERTY = "SourceContext";

    private readonly IReadOnlyDictionary<string, string> _levelMapping;
    private readonly string _defaultSourceContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedLevelEnricher"/> class.
    /// </summary>
    /// <param name="levelMapping">Mapping dictionary from standard level names to display codes.</param>
    /// <param name="defaultSourceContext">Default context name when none is provided on the event.</param>
    public MappedLevelEnricher(
        IReadOnlyDictionary<string, string>? levelMapping,
        string? defaultSourceContext = null)
    {

        _levelMapping = levelMapping ?? new Dictionary<string, string>();
        _defaultSourceContext = string.IsNullOrWhiteSpace(defaultSourceContext)
            ? "App"
            : defaultSourceContext;
    }

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {

        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var levelKey = MapToLevelKey(logEvent.Level);
        var mappedName = _levelMapping.TryGetValue(levelKey, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : levelKey.ToUpperInvariant();

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MAPPED_LEVEL_PROPERTY, mappedName));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(SOURCE_CONTEXT_PROPERTY, _defaultSourceContext));
    }

    private static string MapToLevelKey(LogEventLevel level)
        => level switch
        {
            LogEventLevel.Verbose => "Trace",
            LogEventLevel.Debug => "Debug",
            LogEventLevel.Information => "Information",
            LogEventLevel.Warning => "Warning",
            LogEventLevel.Error => "Error",
            LogEventLevel.Fatal => "Fatal",
            _ => level.ToString()
        };
}
