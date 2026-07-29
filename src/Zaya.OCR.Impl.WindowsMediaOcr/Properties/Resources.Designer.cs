using System.Resources;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Properties;

internal static class Resources
{
    private static readonly ResourceManager _rm =
        new("Zaya.OCR.Impl.WindowsMediaOcr.Properties.Resources", typeof(Resources).Assembly);

    public static ResourceManager ResourceManager => _rm;
}
