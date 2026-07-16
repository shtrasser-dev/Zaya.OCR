using System.Resources;

namespace Zaya.OCR.Impl.OneOcr.Properties;

internal static class Resources
{
    private static readonly ResourceManager _rm =
        new("Zaya.OCR.Impl.OneOcr.Properties.Resources", typeof(Resources).Assembly);

    public static ResourceManager ResourceManager => _rm;
}
