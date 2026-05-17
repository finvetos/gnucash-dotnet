using System.Reflection;
using System.Runtime.CompilerServices;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using FluentAssertions;
using GnuCash.DotNet.Bridge;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ArchArchitecture = ArchUnitNET.Domain.Architecture;
using ReflectionAssembly = System.Reflection.Assembly;
using ReflectionType = System.Type;

namespace GnuCash.DotNet.Architecture.Tests;

public sealed class BoundarySmellTests
{
    private static readonly ReflectionAssembly SdkAssembly = typeof(AssemblyMarker).Assembly;
    private static readonly ReflectionAssembly BridgeAssembly = typeof(CliApplication).Assembly;
    private static readonly ReflectionAssembly ProtocolAssembly = typeof(Protocol.AssemblyMarker).Assembly;

    private static readonly ReflectionAssembly[] ProductionAssemblies =
    [
        SdkAssembly,
        BridgeAssembly,
        ProtocolAssembly
    ];

    private static readonly ArchArchitecture Architecture = new ArchLoader()
        .LoadAssemblies(ProductionAssemblies)
        .Build();

    [Fact]
    public void ProtocolShouldStayPure()
    {
        AssertDoesNotReferenceAssemblies(
            ProtocolAssembly,
            "GnuCash.DotNet",
            "GnuCash.DotNet.Bridge",
            "Microsoft.Extensions.Configuration.Abstractions",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Logging.Abstractions",
            "Microsoft.Extensions.Options",
            "Serilog",
            "Spectre.Console",
            "System.CommandLine");
    }

    [Fact]
    public void SdkShouldNotDependOnBridgeImplementationOrCliRuntime()
    {
        AssertDoesNotReferenceAssemblies(
            SdkAssembly,
            "GnuCash.DotNet.Bridge",
            "Serilog",
            "Spectre.Console",
            "System.CommandLine");
    }

    [Fact]
    public void BridgeShouldNotDependOnSdkImplementation()
    {
        AssertDoesNotReferenceAssemblies(BridgeAssembly, "GnuCash.DotNet");
    }

    [Fact]
    public void SdkPublicSurfaceShouldNotExposeBridgeTypes()
    {
        var leaks = SdkAssembly.GetExportedTypes()
            .SelectMany(GetPublicSurfaceTypes)
            .Where(type => type.Assembly == BridgeAssembly)
            .Select(type => type.FullName)
            .Distinct()
            .Order()
            .ToArray();

        leaks.Should().BeEmpty("the SDK should hide bridge process details behind its own API");
    }

    [Fact]
    public void SdkBookSurfaceShouldNotExposeRawBridgeProtocolBookDtos()
    {
        var forbiddenProtocolTypes = new HashSet<ReflectionType>
        {
            typeof(Protocol.Contracts.GnuCashBookRequest),
            typeof(Protocol.Contracts.GnuCashBookSummary),
            typeof(Protocol.Contracts.GnuCashCommodityRecord),
            typeof(Protocol.Contracts.GnuCashAccountRecord),
            typeof(Protocol.Contracts.GnuCashAmountRecord),
            typeof(Protocol.Contracts.GnuCashSplitRecord),
            typeof(Protocol.Contracts.GnuCashTransactionRecord),
            typeof(Protocol.Contracts.GnuCashPriceRecord)
        };

        var leaks = SdkAssembly.GetExportedTypes()
            .SelectMany(GetPublicSurfaceTypes)
            .Where(forbiddenProtocolTypes.Contains)
            .Select(type => type.FullName)
            .Distinct()
            .Order()
            .ToArray();

        leaks.Should().BeEmpty("book APIs should expose SDK domain models instead of raw bridge protocol contracts");
    }

    [Fact]
    public void ProtocolContractsShouldAvoidMutablePublicSetters()
    {
        var mutableProperties = ProtocolAssembly.GetExportedTypes()
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => new { Type = type, Property = property }))
            .Where(item => item.Property.SetMethod is { IsPublic: true } &&
                           !IsInitOnly(item.Property))
            .Select(item => $"{item.Type.FullName}.{item.Property.Name}")
            .Order()
            .ToArray();

        mutableProperties.Should().BeEmpty("protocol DTOs cross process boundaries and should be immutable value shapes");
    }

    [Fact]
    public void ProductionCodeShouldNotDependOnTestTooling()
    {
        AssertNoProductionDependencyOn("Xunit");
        AssertNoProductionDependencyOn("FluentAssertions");
        AssertNoProductionDependencyOn("AutoFixture");
        AssertNoProductionDependencyOn("NSubstitute");
        AssertNoProductionDependencyOn("ArchUnitNET");
    }

    [Fact]
    public void ProductionTypesShouldStayInOwnedNamespaces()
    {
        AssertTypesStayInNamespace(SdkAssembly, "GnuCash.DotNet");
        AssertTypesStayInNamespace(BridgeAssembly, "GnuCash.DotNet.Bridge");
        AssertTypesStayInNamespace(ProtocolAssembly, "GnuCash.DotNet.Protocol");
    }

    [Fact]
    public void ProductionNamesShouldNotContainTemplateResidue()
    {
        var residueTerms = new[]
        {
            "ClassLibrary",
            "HeurexTemplate",
            "LibraryOptions",
            "LibraryService",
            "Sample",
            "TemplateRequest"
        };

        var residueTypes = ProductionAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.FullName is not null &&
                           residueTerms.Any(term => type.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Select(type => type.FullName)
            .Order()
            .ToArray();

        residueTypes.Should().BeEmpty("template residue tends to spread into public API and docs");
    }

    [Fact]
    public void ProductionAssembliesShouldCarryFinvetosCopyright()
    {
        var missingCopyright = ProductionAssemblies
            .Where(assembly => assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright !=
                               "Copyright (c) Finvetos")
            .Select(assembly => assembly.GetName().Name)
            .Order()
            .ToArray();

        missingCopyright.Should().BeEmpty("released assemblies should carry the project copyright metadata");
    }

    [Fact]
    public void ProductionAssembliesShouldBeStrongNamedWhenReleaseSigningIsExpected()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("GNUCASH_DOTNET_EXPECT_SIGNED"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var unsignedAssemblies = ProductionAssemblies
            .Where(assembly => assembly.GetName().GetPublicKeyToken() is not { Length: > 0 })
            .Select(assembly => assembly.GetName().Name)
            .Order()
            .ToArray();

        unsignedAssemblies.Should().BeEmpty("release CI requires strong-name-signed production assemblies");
    }

    private static void AssertDoesNotReferenceAssemblies(
        ReflectionAssembly sourceAssembly,
        params string[] forbiddenAssemblyNames)
    {
        var references = sourceAssembly.GetReferencedAssemblies()
            .Select(name => name.Name)
            .Where(name => name is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var forbiddenAssemblyName in forbiddenAssemblyNames)
        {
            references.Should().NotContain(
                forbiddenAssemblyName,
                $"{sourceAssembly.GetName().Name} must keep its architecture boundary clean");
        }
    }

    private static void AssertNoProductionDependencyOn(string namespacePrefix)
    {
        Types().Should()
            .NotDependOnAny(Types().That().ResideInNamespace(namespacePrefix))
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    private static void AssertTypesStayInNamespace(ReflectionAssembly assembly, string namespacePrefix)
    {
        assembly.GetTypes()
            .Where(type => !IsGeneratedType(type) &&
                           type.Namespace is not null &&
                           !type.Namespace.Equals(namespacePrefix, StringComparison.Ordinal) &&
                           !type.Namespace.StartsWith(namespacePrefix + ".", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .Should().BeEmpty();
    }

    private static IEnumerable<ReflectionType> GetPublicSurfaceTypes(ReflectionType type)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                foreach (var exposedType in Unwrap(parameter.ParameterType))
                {
                    yield return exposedType;
                }
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            foreach (var exposedType in Unwrap(method.ReturnType))
            {
                yield return exposedType;
            }

            foreach (var parameter in method.GetParameters())
            {
                foreach (var exposedType in Unwrap(parameter.ParameterType))
                {
                    yield return exposedType;
                }
            }
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            foreach (var exposedType in Unwrap(property.PropertyType))
            {
                yield return exposedType;
            }
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            foreach (var exposedType in Unwrap(field.FieldType))
            {
                yield return exposedType;
            }
        }
    }

    private static IEnumerable<ReflectionType> Unwrap(ReflectionType type)
    {
        if (type.IsByRef || type.IsPointer || type.IsArray)
        {
            type = type.GetElementType() ?? type;
        }

        yield return type;

        if (!type.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in Unwrap(argument))
            {
                yield return nested;
            }
        }
    }

    private static bool IsInitOnly(PropertyInfo property)
    {
        return property.SetMethod?.ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(IsExternalInit)) == true;
    }

    private static bool IsGeneratedType(ReflectionType type)
    {
        return type.Namespace?.StartsWith("CompiledAvaloniaXaml", StringComparison.Ordinal) == true ||
               type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
    }
}
