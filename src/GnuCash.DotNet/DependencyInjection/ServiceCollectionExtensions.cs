using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        services.AddSingleton<GnuCashBridgeProcess>();
        services.AddSingleton(serviceProvider => new GnuCashClient(
            serviceProvider.GetRequiredService<ILogger<GnuCashClient>>(),
            serviceProvider.GetRequiredService<IOptions<GnuCashBridgeOptions>>(),
            serviceProvider.GetRequiredService<GnuCashBridgeProcess>()));

        return services;
    }
}
