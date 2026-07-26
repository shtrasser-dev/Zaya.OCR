using Zaya.OCR.Impl.ProximityTextLayout.Services;
using Zaya.OCR.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering ProximityTextLayout services in a dependency injection container.
/// </summary>
public static class ProximityTextLayoutServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ProximityTextLayoutService"/> as a singleton <see cref="ITextLayoutService"/>.
    /// Pass engine settings directly to <see cref="ITextLayoutService.CreateSessionAsync(IReadOnlyDictionary{string, object}, CancellationToken)"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddProximityTextLayout(this IServiceCollection services)
    {
        services.AddSingleton<ITextLayoutService, ProximityTextLayoutService>();
        return services;
    }
}
