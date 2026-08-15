using Zaya.OCR.Impl.WindowsMediaOcr;
using Zaya.OCR.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Windows.Media.Ocr services in a dependency injection container.
/// </summary>
public static class WindowsMediaOcrServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="WindowsMediaOcrService"/> as a singleton <see cref="IOCRService"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddWindowsMediaOcr(this IServiceCollection services)
    {
        services.AddSingleton<IOCRService, WindowsMediaOcrService>();
        return services;
    }
}
