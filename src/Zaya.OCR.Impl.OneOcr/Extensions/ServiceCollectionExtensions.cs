using Zaya.OCR.Impl.OneOcr.Services;
using Zaya.OCR.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering OneOCR services in a dependency injection container.
/// </summary>
public static class OneOcrServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OneOcrService"/> as a singleton <see cref="IOCRService"/>.
    /// Call <see cref="IOCRService.InitializeAsync"/> explicitly after the container is built.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddOneOcr(this IServiceCollection services)
    {
        services.AddSingleton<IOCRService, OneOcrService>();
        return services;
    }
}
