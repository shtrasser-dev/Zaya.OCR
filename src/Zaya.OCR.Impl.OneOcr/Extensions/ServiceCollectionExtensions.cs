using Zaya.OCR.Impl.OneOcr;
using Zaya.OCR.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering OneOCR services in a dependency injection container.
/// </summary>
public static class OneOcrServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OneOcrService"/> as a singleton <see cref="IOCRService"/>.
    /// Pass engine settings directly to <see cref="IOCRService.CreateSessionAsync(IReadOnlyDictionary{string, object}, CancellationToken)"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddOneOcr(this IServiceCollection services)
    {
        services.AddSingleton<IOCRService, OneOcrService>();
        return services;
    }
}
