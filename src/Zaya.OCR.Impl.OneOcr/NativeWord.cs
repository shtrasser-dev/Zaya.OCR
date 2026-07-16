using System.Drawing;
using System.Runtime.InteropServices;

namespace Zaya.OCR.Impl.OneOcr;

internal struct NativeWord
{
    public string Text;
    public Rectangle Bounds;
    public double Confidence;
}
