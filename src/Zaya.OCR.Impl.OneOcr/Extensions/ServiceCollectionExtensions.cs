using Zaya.OCR.Impl.OneOcr.Services;
using Zaya.OCR.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering OneOCR services in a DI container.
/// </summary>
public static class OneOcrServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OneOcrService"/> as a singleton <see cref="IOCRService"/>.
    /// The underlying <see cref="Microsoft.Windows.AI.Imaging.TextRecognizer"/> is initialized
    /// lazily on the first session creation. Sessions are created on demand via
    /// <see cref="IOCRService.CreateSessionAsync"/> and are designed to be short-lived.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddOneOcr(this IServiceCollection services)
    {
        services.AddSingleton<IOCRService, OneOcrService>();
        return services;
    }
}
