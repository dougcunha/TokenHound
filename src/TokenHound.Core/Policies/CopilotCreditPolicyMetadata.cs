using System;
using System.Collections.Generic;
using TokenHound.Core.Models;

namespace TokenHound.Core.Policies;

internal static class CopilotCreditPolicyMetadata
{
    internal static MetadataResult Validate(
        CopilotCreditAggregationRequest request,
        ICollection<string> issues)
    {

        var contextInvalid = !ValidateContext(request.Context, issues);
        var period = ValidatePeriod(request.Period, issues);
        var coverage = ValidateCoverage(request.Coverage, issues);
        var filterInvalid = !ValidateFilter(request.Filter, issues);
        var sourceInvalid = !ValidateSource(request.Source, issues);

        return new MetadataResult(
            contextInvalid || period.IsInvalid || coverage.IsInvalid || filterInvalid || sourceInvalid,
            period.IsPartial || coverage.IsPartial);
    }

    internal static bool IsContextValid(CopilotBillingContext? context)
        => context is not null &&
            !string.IsNullOrWhiteSpace(context.PrincipalId) &&
            !string.IsNullOrWhiteSpace(context.OwnerId) &&
            !string.IsNullOrWhiteSpace(context.EvidenceKey) &&
            IsKnownScope(context.Scope) &&
            IsKnownPlan(context.Plan);

    internal static bool IsPeriodComplete(CopilotBillingPeriod period)
        => IsPeriodStructurallyValid(period) &&
            period.IsVerified &&
            period.StartUtc.HasValue &&
            period.EndExclusiveUtc.HasValue &&
            period.RequestedYear > 0 &&
            period.RequestedMonth > 0 &&
            period.StartUtc.Value.UtcDateTime.Year == period.RequestedYear &&
            period.StartUtc.Value.UtcDateTime.Month == period.RequestedMonth;

    internal static bool CoverageMatchesPeriod(
        CopilotReportCoverage coverage,
        CopilotBillingPeriod period)
    {

        if (!HasCompleteCoverage(coverage) || !IsPeriodComplete(period))
            return false;

        var expectedStart = DateOnly.FromDateTime(period.StartUtc!.Value.UtcDateTime);
        var expectedEnd = DateOnly.FromDateTime(period.EndExclusiveUtc!.Value.UtcDateTime).AddDays(-1);

        return coverage.StartDay == expectedStart && coverage.EndDay == expectedEnd;
    }

    internal static bool ContextMatches(CopilotBillingContext left, CopilotBillingContext right)
        => string.Equals(left.PrincipalId.Trim(), right.PrincipalId.Trim(), StringComparison.OrdinalIgnoreCase) &&
            left.Scope == right.Scope &&
            IdentifierMatches(left.OwnerId, right.OwnerId);

    internal static bool PeriodMatches(CopilotBillingPeriod left, CopilotBillingPeriod right)
        => left.RequestedYear == right.RequestedYear &&
            left.RequestedMonth == right.RequestedMonth &&
            left.StartUtc == right.StartUtc &&
            left.EndExclusiveUtc == right.EndExclusiveUtc;

    internal static bool FiltersMatch(
        IReadOnlyDictionary<string, string>? left,
        IReadOnlyDictionary<string, string>? right)
    {

        if (left is null || right is null || left.Count != right.Count)
            return false;

        foreach (var pair in left)
        {

            if (pair.Key is null || pair.Value is null ||
                !FilterValueMatches(right, pair.Key, pair.Value))
                return false;
        }

        return true;
    }

    internal static bool IdentifierMatches(string? actual, string? expected)
        => !string.IsNullOrWhiteSpace(actual) &&
            !string.IsNullOrWhiteSpace(expected) &&
            string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);

    internal static bool FilterValueMatches(
        IReadOnlyDictionary<string, string>? filters,
        string key,
        string expected)
        => !string.IsNullOrWhiteSpace(expected) &&
            filters is not null &&
            TryGetFilterValue(filters, key, out var value) &&
            value is not null &&
            string.Equals(expected.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase);

    internal static CopilotBillingContext CreateUnknownContext()
        => new()
        {
            PrincipalId = string.Empty,
            Scope = CopilotBillingScope.Unknown,
            Plan = CopilotPlanType.Unknown
        };

    private static bool IsPeriodStructurallyValid(CopilotBillingPeriod? period)
        => period is not null &&
            (!period.StartUtc.HasValue || !period.EndExclusiveUtc.HasValue ||
                period.EndExclusiveUtc.Value > period.StartUtc.Value) &&
            period.RequestedYear is >= 0 and <= 9999 &&
            period.RequestedMonth is >= 0 and <= 12;

    private static bool IsCoverageStructurallyValid(CopilotReportCoverage? coverage)
        => coverage is not null &&
            (coverage.StartDay is null || coverage.EndDay is null ||
                coverage.EndDay.Value >= coverage.StartDay.Value) &&
            coverage.MissingDays is not null &&
            coverage.Filters is not null;

    private static bool HasCompleteCoverage(CopilotReportCoverage coverage)
        => coverage.IsComplete &&
            coverage.StartDay.HasValue &&
            coverage.EndDay.HasValue &&
            coverage.MissingDays.Count == 0 &&
            !coverage.HasMissingPartitions &&
            !coverage.HasInvalidRows;

    private static bool IsFilterValid(CopilotCreditFilter? filter)
        => filter is not null &&
            !string.IsNullOrWhiteSpace(filter.Product) &&
            !string.IsNullOrWhiteSpace(filter.UnitType) &&
            (filter.Sku is null || !string.IsNullOrWhiteSpace(filter.Sku)) &&
            (filter.Model is null || !string.IsNullOrWhiteSpace(filter.Model));

    private static bool IsSourceValid(CopilotCreditSource source)
        => source is CopilotCreditSource.BillingApi or CopilotCreditSource.DailyUserReport;

    private static bool IsKnownScope(CopilotBillingScope scope)
        => scope is CopilotBillingScope.Personal or CopilotBillingScope.Organization or CopilotBillingScope.Enterprise;

    private static bool IsKnownPlan(CopilotPlanType plan)
        => plan is CopilotPlanType.Unknown or CopilotPlanType.Free or CopilotPlanType.Pro or
            CopilotPlanType.ProPlus or CopilotPlanType.Max or CopilotPlanType.Business or CopilotPlanType.Enterprise;

    private static bool ValidateContext(CopilotBillingContext? context, ICollection<string> issues)
    {

        if (IsContextValid(context))
            return true;

        issues.Add("ContextInvalid");

        return false;
    }

    private static MetadataResult ValidatePeriod(
        CopilotBillingPeriod? period,
        ICollection<string> issues)
    {

        if (!IsPeriodStructurallyValid(period))
        {

            issues.Add("PeriodInvalid");

            return new MetadataResult(true, false);
        }

        if (IsPeriodComplete(period!))
            return default;

        issues.Add("PeriodUnverified");

        return new MetadataResult(false, true);
    }

    private static MetadataResult ValidateCoverage(
        CopilotReportCoverage? coverage,
        ICollection<string> issues)
    {

        if (!IsCoverageStructurallyValid(coverage))
        {

            issues.Add("CoverageInvalid");

            return new MetadataResult(true, false);
        }

        if (HasCompleteCoverage(coverage!))
            return default;

        issues.Add("CoveragePartial");

        return new MetadataResult(false, true);
    }

    private static bool ValidateFilter(CopilotCreditFilter? filter, ICollection<string> issues)
    {

        if (IsFilterValid(filter))
            return true;

        issues.Add("FilterInvalid");

        return false;
    }

    private static bool ValidateSource(CopilotCreditSource source, ICollection<string> issues)
    {

        if (IsSourceValid(source))
            return true;

        issues.Add("SourceInvalid");

        return false;
    }

    private static bool TryGetFilterValue(
        IReadOnlyDictionary<string, string> filters,
        string key,
        out string? value)
    {

        foreach (var pair in filters)
        {

            if (pair.Key is not null && key is not null &&
                string.Equals(pair.Key.Trim(), key.Trim(), StringComparison.OrdinalIgnoreCase))
            {

                value = pair.Value;

                return true;
            }
        }

        value = string.Empty;

        return false;
    }

    internal static bool HasAllowanceEvidence(CopilotAllowanceEvidence allowance)
        => !string.IsNullOrWhiteSpace(allowance.JsonPath) &&
            Uri.TryCreate(allowance.DocumentationUrl, UriKind.Absolute, out var documentationUri) &&
            documentationUri.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrWhiteSpace(allowance.EvidenceKey) &&
            !string.IsNullOrWhiteSpace(allowance.Unit);

    internal static bool AreRequestedFiltersCovered(
        CopilotCreditFilter? filter,
        IReadOnlyDictionary<string, string>? allowanceFilters)
        => filter is not null &&
            IsOptionalFilterCovered("sku", filter.Sku, allowanceFilters) &&
            IsOptionalFilterCovered("model", filter.Model, allowanceFilters);

    private static bool IsOptionalFilterCovered(
        string key,
        string? expected,
        IReadOnlyDictionary<string, string>? filters)
        => expected is null ||
            filters is not null &&
            FilterValueMatches(filters, key, expected);

    internal readonly record struct MetadataResult(bool IsInvalid, bool IsPartial);
}
