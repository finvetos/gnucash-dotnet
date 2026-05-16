using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GnuCash.DotNet.DependencyInjection;

/// <summary>
/// Registers GnuCash.DotNet services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the GnuCash .NET SDK services and bridge options.
    /// </summary>
    public static IServiceCollection AddGnuCashDotNet(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<GnuCashBridgeOptions>()
            .Bind(configuration.GetSection(GnuCashBridgeOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<GnuCashClient>();

        return services;
    }
}