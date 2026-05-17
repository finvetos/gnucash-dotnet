using System.Text.Json;
using FluentAssertions;
using GnuCash.DotNet.Protocol.Contracts;
using Xunit;

namespace GnuCash.DotNet.Architecture.Tests;

public sealed class CapabilityCoverageTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] AllowedStatuses =
    [
        "sdk-supported",
        "bridge-supported",
        "read-only",
        "write-supported",
        "import-export",
        "deferred",
        "not-viable-with-stock-install",
        "excluded-ui",
        "not-assessed"
    ];

    private static readonly string[] RequiredCoverageDimensions =
    [
        "sdk-supported",
        "write-supported",
        "bridge-supported",
        "read-only"
    ];

    [Fact]
    public void CapabilityCoverageMatrixShouldBeWellFormed()
    {
        var coverage = ReadCoverage();

        coverage.Version.Should().Be(1);
        coverage.Statuses.Should().BeEquivalentTo(AllowedStatuses);
        coverage.Capabilities.Should().NotBeEmpty();
        coverage.Capabilities.Select(capability => capability.Id)
            .Should().OnlyHaveUniqueItems("capability ids are used as stable release-gate keys");

        coverage.Capabilities.Should().OnlyContain(capability =>
            !string.IsNullOrWhiteSpace(capability.Id) &&
            !string.IsNullOrWhiteSpace(capability.Domain) &&
            !string.IsNullOrWhiteSpace(capability.Name) &&
            !string.IsNullOrWhiteSpace(capability.Status) &&
            !string.IsNullOrWhiteSpace(capability.Surface) &&
            !string.IsNullOrWhiteSpace(capability.Evidence));

        coverage.Capabilities.Select(capability => capability.Status)
            .Should().OnlyContain(status => AllowedStatuses.Contains(status));
    }

    [Fact]
    public void SupportedCapabilitiesShouldCarryVerificationEvidence()
    {
        var coverage = ReadCoverage();
        var supportedStatuses = new[]
        {
            "sdk-supported",
            "bridge-supported",
            "read-only",
            "write-supported",
            "import-export"
        };

        var missingVerification = coverage.Capabilities
            .Where(capability => supportedStatuses.Contains(capability.Status))
            .Where(capability => string.IsNullOrWhiteSpace(capability.Verification))
            .Select(capability => capability.Id)
            .Order()
            .ToArray();

        missingVerification.Should().BeEmpty("supported capability entries should name their verification path");
    }

    [Fact]
    public void WriteSupportedCapabilitiesShouldNameReopenOrValidatorEvidence()
    {
        var coverage = ReadCoverage();
        var weakWriteEvidence = coverage.Capabilities
            .Where(capability => capability.Status == "write-supported")
            .Where(capability =>
                !Contains(capability.Verification, "reopen") &&
                !Contains(capability.Verification, "validator"))
            .Select(capability => capability.Id)
            .Order()
            .ToArray();

        weakWriteEvidence.Should().BeEmpty("write support must be tied to save/reopen verification or a native validator");
    }

    [Fact]
    public void CurrentBridgeProtocolCommandsShouldBeRepresented()
    {
        var coverage = ReadCoverage();
        var coverageText = string.Join(
            Environment.NewLine,
            coverage.Capabilities.Select(capability => capability.Surface + " " + capability.Evidence));

        foreach (var requestKind in Enum.GetNames<BridgeRequestKind>())
        {
            coverageText.Should().Contain(
                "BridgeRequestKind." + requestKind,
                $"the capability matrix should map bridge protocol command {requestKind}");
        }
    }

    [Fact]
    public void CapabilityCoverageDimensionsShouldUseKnownCapabilityIds()
    {
        var coverage = ReadCoverage();
        var dimensions = ReadCoverageDimensions();
        var capabilityIds = coverage.Capabilities
            .Select(capability => capability.Id)
            .ToHashSet(StringComparer.Ordinal);

        dimensions.Version.Should().Be(2);
        dimensions.TargetThreshold.Should().BeGreaterThanOrEqualTo(0.8);
        dimensions.Dimensions.Select(dimension => dimension.Name)
            .Should().BeEquivalentTo(RequiredCoverageDimensions);

        foreach (var dimension in dimensions.Dimensions)
        {
            dimension.TargetCapabilities.Should().OnlyHaveUniqueItems(
                $"{dimension.Name} target ids should remain stable");
            dimension.CoveredCapabilities.Should().OnlyHaveUniqueItems(
                $"{dimension.Name} covered ids should remain stable");
            dimension.TargetCapabilities.Should().OnlyContain(
                capabilityId => capabilityIds.Contains(capabilityId),
                $"{dimension.Name} targets should reference known capability ids");
            dimension.CoveredCapabilities.Should().OnlyContain(
                capabilityId => capabilityIds.Contains(capabilityId),
                $"{dimension.Name} covered ids should reference known capability ids");
            dimension.CoveredCapabilities.Should().BeSubsetOf(
                dimension.TargetCapabilities,
                $"{dimension.Name} coverage must be measured against the declared target set");
        }
    }

    [Fact]
    public void OverallNonUiCapabilityCoverageShouldMeetReleaseThreshold()
    {
        var coverage = ReadCoverage();
        var dimensions = ReadCoverageDimensions();
        var supportedStatuses = new[]
        {
            "sdk-supported",
            "bridge-supported",
            "read-only",
            "write-supported",
            "import-export"
        };
        var nonUiCapabilities = coverage.Capabilities
            .Where(capability => capability.Status != "excluded-ui")
            .ToArray();
        var supported = nonUiCapabilities
            .Count(capability => supportedStatuses.Contains(capability.Status));
        var ratio = supported / (double)nonUiCapabilities.Length;

        ratio.Should().BeGreaterThanOrEqualTo(
            dimensions.TargetThreshold,
            "overall non-UI GnuCash capability coverage should not use a scoped denominator");
    }

    private static CapabilityCoverageDocument ReadCoverage()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "gnucash-dotnet-docs",
            "capabilities",
            "capability-coverage.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<CapabilityCoverageDocument>(json, JsonOptions) ??
               throw new InvalidOperationException("Capability coverage JSON could not be parsed.");
    }

    private static CapabilityCoverageDimensionsDocument ReadCoverageDimensions()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "gnucash-dotnet-docs",
            "capabilities",
            "capability-coverage-dimensions.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<CapabilityCoverageDimensionsDocument>(json, JsonOptions) ??
               throw new InvalidOperationException("Capability coverage dimensions JSON could not be parsed.");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GnuCash.DotNet.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static bool Contains(string value, string expected) =>
        value.Contains(expected, StringComparison.OrdinalIgnoreCase);

    private sealed record CapabilityCoverageDocument(
        int Version,
        string Updated,
        IReadOnlyList<string> Statuses,
        IReadOnlyList<CapabilityEntry> Capabilities);

    private sealed record CapabilityEntry(
        string Id,
        string Domain,
        string Name,
        string Status,
        string Surface,
        string Evidence,
        string Verification);

    private sealed record CapabilityCoverageDimensionsDocument(
        int Version,
        string Updated,
        double TargetThreshold,
        IReadOnlyList<CapabilityCoverageDimension> Dimensions);

    private sealed record CapabilityCoverageDimension(
        string Name,
        string Description,
        IReadOnlyList<string> TargetCapabilities,
        IReadOnlyList<string> CoveredCapabilities);
}
