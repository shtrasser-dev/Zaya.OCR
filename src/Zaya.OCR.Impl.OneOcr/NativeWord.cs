using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.OneOcr;

internal struct NativeWord
{
    public string Text;
    public BoundingBox Bounds;
    public double Confidence;
}
