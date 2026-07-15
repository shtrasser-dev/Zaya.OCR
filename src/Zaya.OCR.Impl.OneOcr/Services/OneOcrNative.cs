using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Zaya.OCR.Impl.OneOcr.Services;

/// <summary>
/// Low-level P/Invoke wrapper around oneocr.dll (Windows 11 SnippingTool OCR engine).
/// Loads the DLL from the SnippingTool or WindowsAppSDK package folder at runtime.
/// </summary>
internal static class OneOcrNative
{
    private const string ModelKey = "kj)TGtrK>f]b[Piow.gU+nC@s\"\"\"\"\"\"4";

    private const long S_OK = 0;

#pragma warning disable CS0649
    private struct Img
    {
        public int t;
        public int col;
        public int row;
        public int _unk;
        public long step;
        public long data_ptr;
    }
#pragma warning restore CS0649

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long CreateOcrInitOptions(out long ctx);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long OcrInitOptionsSetUseModelDelayLoad(long ctx, byte useDelayLoad);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long CreateOcrPipeline(long modelPath, long key, long ctx, out long pipeline);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long CreateOcrProcessOptions(out long opt);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long OcrProcessOptionsSetMaxRecognitionLineCount(long opt, long maxCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long RunOcrPipeline(long pipeline, ref Img img, long opt, out long instance);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GetOcrLineCount(long instance, out long count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GetOcrLine(long instance, long index, out long line);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GetOcrLineContent(long line, out long content);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GetOcrLineWordCount(long line, out long count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GetOcrWord(long line, long index, out long word);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GetOcrWordContent(long word, out long content);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GetOcrWordBoundingBox(long word, out long box);

    private static readonly nint _dllHandle;
    private static readonly string? _modelPath;
    private static readonly CreateOcrInitOptions? _createInit;
    private static readonly OcrInitOptionsSetUseModelDelayLoad? _setDelayLoad;
    private static readonly CreateOcrPipeline? _createPipeline;
    private static readonly CreateOcrProcessOptions? _createOpt;
    private static readonly OcrProcessOptionsSetMaxRecognitionLineCount? _setMaxLine;
    private static readonly RunOcrPipeline? _runPipeline;
    private static readonly GetOcrLineCount? _getLineCount;
    private static readonly GetOcrLine? _getLine;
    private static readonly GetOcrLineContent? _getLineContent;
    private static readonly GetOcrLineWordCount? _getLineWordCount;
    private static readonly GetOcrWord? _getWord;
    private static readonly GetOcrWordContent? _getWordContent;
    private static readonly GetOcrWordBoundingBox? _getWordBoundingBox;

    private static long _ctx;
    private static long _pipeline;
    private static readonly object _initLock = new();
    private static bool _initialized;
    private static bool _initFailed;

    static OneOcrNative()
    {
        try
        {
            File.AppendAllText(@"D:\p\Zaya\debug.txt", $"[{DateTime.Now:T}] OneOcrNative static ctor start\n");

            var dllPath = FindOneOcrDll();
            File.AppendAllText(@"D:\p\Zaya\debug.txt", $"  FindOneOcrDll: {(dllPath ?? "null")}\n");
            if (dllPath is null)
                return;

            dllPath = CopyToWritableLocation(dllPath);
            File.AppendAllText(@"D:\p\Zaya\debug.txt", $"  CopyToWritableLocation: {(dllPath ?? "null")}\n");
            if (dllPath is null)
                return;

            _modelPath = FindModelFile(Path.GetDirectoryName(dllPath)!);
            File.AppendAllText(@"D:\p\Zaya\debug.txt", $"  FindModelFile: {(_modelPath ?? "null")}\n");
            if (_modelPath is null)
                return;

            _dllHandle = LoadLibrary(dllPath);
            File.AppendAllText(@"D:\p\Zaya\debug.txt", $"  LoadLibrary: 0x{_dllHandle:X}\n");
            if (_dllHandle == 0)
                return;

            _createInit = GetDelegate<CreateOcrInitOptions>("CreateOcrInitOptions");
            _setDelayLoad = GetDelegate<OcrInitOptionsSetUseModelDelayLoad>("OcrInitOptionsSetUseModelDelayLoad");
            _createPipeline = GetDelegate<CreateOcrPipeline>("CreateOcrPipeline");
            _createOpt = GetDelegate<CreateOcrProcessOptions>("CreateOcrProcessOptions");
            _setMaxLine = GetDelegate<OcrProcessOptionsSetMaxRecognitionLineCount>("OcrProcessOptionsSetMaxRecognitionLineCount");
            _runPipeline = GetDelegate<RunOcrPipeline>("RunOcrPipeline");
            _getLineCount = GetDelegate<GetOcrLineCount>("GetOcrLineCount");
            _getLine = GetDelegate<GetOcrLine>("GetOcrLine");
            _getLineContent = GetDelegate<GetOcrLineContent>("GetOcrLineContent");
            _getLineWordCount = GetDelegate<GetOcrLineWordCount>("GetOcrLineWordCount");
            _getWord = GetDelegate<GetOcrWord>("GetOcrWord");
            _getWordContent = GetDelegate<GetOcrWordContent>("GetOcrWordContent");
            _getWordBoundingBox = GetDelegate<GetOcrWordBoundingBox>("GetOcrWordBoundingBox");

            File.AppendAllText(@"D:\p\Zaya\debug.txt", $"  OneOcrNative: OK, IsAvailable={_createInit is not null}\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(@"D:\p\Zaya\debug.txt", $"[{DateTime.Now:T}] OneOcrNative error: {ex.GetType().Name}: {ex.Message}\n");
        }
    }

    public static bool IsAvailable => _createInit is not null;

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        if (_initFailed)
            throw new InvalidOperationException("OneOCR native initialization previously failed.");

        lock (_initLock)
        {
            if (_initialized) return;
            if (_initFailed) throw new InvalidOperationException("OneOCR native initialization previously failed.");

            try
            {
                long res = _createInit!(out _ctx);
                if (res != S_OK) throw new Exception($"CreateOcrInitOptions: {res}");

                res = _setDelayLoad!(_ctx, 0);
                if (res != S_OK) throw new Exception($"OcrInitOptionsSetUseModelDelayLoad: {res}");

                if (_modelPath is null)
                    throw new InvalidOperationException("OneOCR model path is null. Ensure FindOneOcrDll found the model.");

                var modelPathBytes = System.Text.Encoding.ASCII.GetBytes(_modelPath + "\0");
                var keyBytes = System.Text.Encoding.ASCII.GetBytes(ModelKey + "\0");

                unsafe
                {
                    fixed (byte* mp = modelPathBytes)
                    fixed (byte* kp = keyBytes)
                    {
                        res = _createPipeline!((long)mp, (long)kp, _ctx, out _pipeline);
                    }
                }

                if (res != S_OK)
                    throw new Exception($"CreateOcrPipeline: {res} (check model key compatibility)");

                _initialized = true;
            }
            catch
            {
                _initFailed = true;
                throw;
            }
        }
    }

    /// <summary>
    /// Performs OCR on BGRA32 pixel data.
    /// </summary>
    public static unsafe NativeWord[] Recognize(byte[] bgraPixels, int width, int height, int stride)
    {
        EnsureInitialized();

        long res = _createOpt!(out long opt);
        if (res != S_OK) throw new Exception($"CreateOcrProcessOptions: {res}");

        res = _setMaxLine!(opt, 1000);
        if (res != S_OK) throw new Exception($"OcrProcessOptionsSetMaxRecognitionLineCount: {res}");

        var img = new Img
        {
            t = 3,
            col = width,
            row = height,
            _unk = 0,
            step = stride,
            data_ptr = 0
        };

        long instance;
        fixed (byte* p = bgraPixels)
        {
            img.data_ptr = (long)p;
            res = _runPipeline!(_pipeline, ref img, opt, out instance);
        }

        if (res != S_OK) throw new Exception($"RunOcrPipeline: {res}");
        if (instance == 0) return [];

        res = _getLineCount!(instance, out long lineCount);
        if (res != S_OK || lineCount == 0) return [];

        var words = new List<NativeWord>();

        for (long li = 0; li < lineCount; li++)
        {
            res = _getLine!(instance, li, out long line);
            if (res != S_OK || line == 0) continue;

            res = _getLineWordCount!(line, out long wordCount);
            if (res != S_OK) continue;

            for (long wi = 0; wi < wordCount; wi++)
            {
                res = _getWord!(line, wi, out long word);
                if (res != S_OK || word == 0) continue;

                res = _getWordContent!(word, out long contentPtr);
                var text = Marshal.PtrToStringAnsi((nint)contentPtr) ?? "";

                res = _getWordBoundingBox!(word, out long boxPtr);
                var bounds = ParseBoundingBox((nint)boxPtr);

                words.Add(new NativeWord
                {
                    Text = text,
                    Bounds = bounds,
                    Confidence = 1.0
                });
            }
        }

        return [.. words];
    }

    private static unsafe Rectangle ParseBoundingBox(nint ptr)
    {
        if (ptr == 0)
            return Rectangle.Empty;

        // BoundingBox: 4 int32 values [left, top, right, bottom]
        var data = (int*)ptr;
        int left = data[0];
        int top = data[1];
        int right = data[2];
        int bottom = data[3];

        return new Rectangle(left, top, right - left, bottom - top);
    }

    private static string? CopyToWritableLocation(string dllPath)
    {
        try
        {
            var targetDir = Path.Combine(Path.GetTempPath(), "Zaya", "OneOcr");
            Directory.CreateDirectory(targetDir);

            var sourceDir = Path.GetDirectoryName(dllPath)!;

            // Copy oneocr.dll
            var targetDll = Path.Combine(targetDir, "oneocr.dll");
            if (!File.Exists(targetDll) || File.GetLastWriteTime(targetDll) < File.GetLastWriteTime(dllPath))
                File.Copy(dllPath, targetDll, overwrite: true);

            // Copy onnxruntime.dll (required by oneocr.dll)
            CopyDep(sourceDir, targetDir, "onnxruntime.dll");
            CopyDep(sourceDir, targetDir, "onnxruntime_providers_shared.dll");

            // Copy oneocr.onemodel (required by CreateOcrPipeline)
            CopyDep(sourceDir, targetDir, "oneocr.onemodel");

            return targetDll;
        }
        catch (Exception ex)
        {
            File.AppendAllText(@"D:\p\Zaya\debug.txt", $"  CopyToWritableLocation error: {ex.Message}\n");
            return dllPath; // fallback to original path
        }
    }

    private static void CopyDep(string sourceDir, string targetDir, string fileName)
    {
        var src = Path.Combine(sourceDir, fileName);
        var dst = Path.Combine(targetDir, fileName);
        if (File.Exists(src) && (!File.Exists(dst) || File.GetLastWriteTime(dst) < File.GetLastWriteTime(src)))
            File.Copy(src, dst, overwrite: true);
    }

    private static string? FindOneOcrDll()
    {
        // 1) App base directory
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oneocr.dll");
            if (File.Exists(path)) return path;
        }
        catch { }

        // 2) Get-AppxPackage via PowerShell (no admin rights needed)
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-Command \"&{(Get-AppxPackage -Name Microsoft.ScreenSketch).InstallLocation}\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is not null)
            {
                var output = proc.StandardOutput.ReadToEnd().Trim();
                if (!string.IsNullOrEmpty(output))
                {
                    var path = Path.Combine(output, "SnippingTool", "oneocr.dll");
                    if (File.Exists(path)) return path;
                }
            }
        }
        catch { }

        return null;
    }

    private static string? FindModelFile(string dllDir)
    {
        // Model is in the same directory as oneocr.dll
        var path = Path.Combine(dllDir, "oneocr.onemodel");
        if (File.Exists(path)) return path;

        // Check parent directory
        var parent = Directory.GetParent(dllDir);
        if (parent is not null)
        {
            path = Path.Combine(parent.FullName, "oneocr.onemodel");
            if (File.Exists(path)) return path;
        }

        return null;
    }

    private static TDelegate GetDelegate<TDelegate>(string name) where TDelegate : Delegate
    {
        var ptr = GetProcAddress(_dllHandle, name);
        if (ptr == 0) return null!;
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr);
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadLibrary(string lpFileName);

    [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern nint GetProcAddress(nint hModule, string lpProcName);
}

internal struct NativeWord
{
    public string Text;
    public Rectangle Bounds;
    public double Confidence;
}
