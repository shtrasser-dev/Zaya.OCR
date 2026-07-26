using System.Drawing;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Zaya.OCR.Impl.OneOcr;

internal sealed class OneOcrEngine : IDisposable
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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GetOcrWordConfidence(long word, out float confidence);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ReleaseOcrResult(long instance);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ReleaseOcrInitOptions(long ctx);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ReleaseOcrPipeline(long pipeline);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ReleaseOcrProcessOptions(long opt);

    private nint _dllHandle;
    private readonly string _modelPath;
    private readonly CreateOcrInitOptions _createInit;
    private readonly OcrInitOptionsSetUseModelDelayLoad _setDelayLoad;
    private readonly CreateOcrPipeline _createPipeline;
    private readonly CreateOcrProcessOptions _createOpt;
    private readonly OcrProcessOptionsSetMaxRecognitionLineCount _setMaxLine;
    private readonly RunOcrPipeline _runPipeline;
    private readonly GetOcrLineCount _getLineCount;
    private readonly GetOcrLine _getLine;
    private readonly GetOcrLineContent _getLineContent;
    private readonly GetOcrLineWordCount _getLineWordCount;
    private readonly GetOcrWord _getWord;
    private readonly GetOcrWordContent _getWordContent;
    private readonly GetOcrWordBoundingBox _getWordBoundingBox;
    private readonly GetOcrWordConfidence? _getWordConfidence;
    private readonly ReleaseOcrResult? _releaseResult;
    private readonly ReleaseOcrInitOptions? _releaseInit;
    private readonly ReleaseOcrPipeline? _releasePipeline;
    private readonly ReleaseOcrProcessOptions? _releaseOpt;

    private long _ctx;
    private long _pipeline;
    private readonly object _initLock = new();
    private bool _initialized;
    private bool _initFailed;
    private bool _disposed;

    public bool IsAvailable { get; }
    public string? ModelPath => _modelPath;

    private OneOcrEngine(
        nint dllHandle,
        string modelPath,
        CreateOcrInitOptions createInit,
        OcrInitOptionsSetUseModelDelayLoad setDelayLoad,
        CreateOcrPipeline createPipeline,
        CreateOcrProcessOptions createOpt,
        OcrProcessOptionsSetMaxRecognitionLineCount setMaxLine,
        RunOcrPipeline runPipeline,
        GetOcrLineCount getLineCount,
        GetOcrLine getLine,
        GetOcrLineContent getLineContent,
        GetOcrLineWordCount getLineWordCount,
        GetOcrWord getWord,
        GetOcrWordContent getWordContent,
        GetOcrWordBoundingBox getWordBoundingBox,
        GetOcrWordConfidence? getWordConfidence,
        ReleaseOcrResult? releaseResult,
        ReleaseOcrInitOptions? releaseInit,
        ReleaseOcrPipeline? releasePipeline,
        ReleaseOcrProcessOptions? releaseOpt)
    {
        _dllHandle = dllHandle;
        _modelPath = modelPath;
        _createInit = createInit;
        _setDelayLoad = setDelayLoad;
        _createPipeline = createPipeline;
        _createOpt = createOpt;
        _setMaxLine = setMaxLine;
        _runPipeline = runPipeline;
        _getLineCount = getLineCount;
        _getLine = getLine;
        _getLineContent = getLineContent;
        _getLineWordCount = getLineWordCount;
        _getWord = getWord;
        _getWordContent = getWordContent;
        _getWordBoundingBox = getWordBoundingBox;
        _getWordConfidence = getWordConfidence;
        _releaseResult = releaseResult;
        _releaseInit = releaseInit;
        _releasePipeline = releasePipeline;
        _releaseOpt = releaseOpt;
        IsAvailable = true;
    }

    public static OneOcrEngine CreateFromSnippingTool(string? cacheDir = null)
    {
        var targetDir = cacheDir ?? Path.Combine(Path.GetTempPath(), "Zaya", "OneOcr");
        Directory.CreateDirectory(targetDir);

        // Prefer an already-populated cache so hosts can run without SnippingTool installed.
        if (TryResolveCachedEngine(targetDir, out var cachedDll, out var cachedModel))
            return CreateFromDll(cachedDll, cachedModel);

        var dllPath = FindOneOcrDll();
        if (dllPath is null)
            throw new OneOcrSnippingToolNotFoundException();

        dllPath = CopyToWritableLocation(dllPath, targetDir);

        var modelPath = FindModelFile(Path.GetDirectoryName(dllPath)!);
        if (modelPath is null)
            throw new OneOcrModelNotFoundException();

        return CreateFromDll(dllPath, modelPath);
    }

    public static OneOcrEngine CreateFromDirectory(string dllDir)
    {
        var dllPath = Path.Combine(dllDir, "oneocr.dll");
        if (!File.Exists(dllPath))
            throw new OneOcrDllNotFoundException();

        var modelPath = FindModelFile(dllDir);
        if (modelPath is null)
            throw new OneOcrModelNotFoundException();

        return CreateFromDll(dllPath, modelPath);
    }

    public static async Task<OneOcrEngine> CreateFromUrlAsync(string url, string? cacheDir = null, CancellationToken ct = default)
    {
        cacheDir ??= Path.Combine(Path.GetTempPath(), "Zaya", "OneOcr");
        Directory.CreateDirectory(cacheDir);

        if (TryResolveCachedEngine(cacheDir, out var dllPath, out var modelPath))
            return CreateFromDll(dllPath, modelPath);

        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            using var http = new HttpClient();
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            await using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                await response.Content.CopyToAsync(fs, ct);
                await fs.FlushAsync(ct);
            }

            ZipFile.ExtractToDirectory(tempFile, cacheDir, overwriteFiles: true);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        if (!TryResolveCachedEngine(cacheDir, out dllPath, out modelPath))
        {
            if (!File.Exists(Path.Combine(cacheDir, "oneocr.dll")))
                throw new OneOcrDllNotFoundException();
            throw new OneOcrModelNotFoundException();
        }

        return CreateFromDll(dllPath, modelPath);
    }

    /// <summary>
    /// Returns true when the cache already has the files needed to load OneOCR:
    /// <c>oneocr.dll</c>, <c>onnxruntime.dll</c>, and <c>oneocr.onemodel</c>.
    /// </summary>
    private static bool TryResolveCachedEngine(string cacheDir, out string dllPath, out string modelPath)
    {
        dllPath = Path.Combine(cacheDir, "oneocr.dll");
        var onnxPath = Path.Combine(cacheDir, "onnxruntime.dll");
        var foundModel = FindModelFile(cacheDir);

        if (File.Exists(dllPath) && File.Exists(onnxPath) && foundModel is not null)
        {
            modelPath = foundModel;
            return true;
        }

        dllPath = null!;
        modelPath = null!;
        return false;
    }

    private static OneOcrEngine CreateFromDll(string dllPath, string modelPath)
    {
        var dllHandle = LoadLibraryEx(dllPath, 0, LOAD_WITH_ALTERED_SEARCH_PATH);
        if (dllHandle == 0)
            throw new OneOcrDllLoadException();

        var createInit = GetDelegate<CreateOcrInitOptions>(dllHandle, "CreateOcrInitOptions")!;
        var setDelayLoad = GetDelegate<OcrInitOptionsSetUseModelDelayLoad>(dllHandle, "OcrInitOptionsSetUseModelDelayLoad")!;
        var createPipeline = GetDelegate<CreateOcrPipeline>(dllHandle, "CreateOcrPipeline")!;
        var createOpt = GetDelegate<CreateOcrProcessOptions>(dllHandle, "CreateOcrProcessOptions")!;
        var setMaxLine = GetDelegate<OcrProcessOptionsSetMaxRecognitionLineCount>(dllHandle, "OcrProcessOptionsSetMaxRecognitionLineCount")!;
        var runPipeline = GetDelegate<RunOcrPipeline>(dllHandle, "RunOcrPipeline")!;
        var getLineCount = GetDelegate<GetOcrLineCount>(dllHandle, "GetOcrLineCount")!;
        var getLine = GetDelegate<GetOcrLine>(dllHandle, "GetOcrLine")!;
        var getLineContent = GetDelegate<GetOcrLineContent>(dllHandle, "GetOcrLineContent")!;
        var getLineWordCount = GetDelegate<GetOcrLineWordCount>(dllHandle, "GetOcrLineWordCount")!;
        var getWord = GetDelegate<GetOcrWord>(dllHandle, "GetOcrWord")!;
        var getWordContent = GetDelegate<GetOcrWordContent>(dllHandle, "GetOcrWordContent")!;
        var getWordBoundingBox = GetDelegate<GetOcrWordBoundingBox>(dllHandle, "GetOcrWordBoundingBox")!;
        var getWordConfidence = GetDelegate<GetOcrWordConfidence>(dllHandle, "GetOcrWordConfidence");
        var releaseResult = GetDelegate<ReleaseOcrResult>(dllHandle, "ReleaseOcrResult");
        var releaseInit = GetDelegate<ReleaseOcrInitOptions>(dllHandle, "ReleaseOcrInitOptions");
        var releasePipeline = GetDelegate<ReleaseOcrPipeline>(dllHandle, "ReleaseOcrPipeline");
        var releaseOpt = GetDelegate<ReleaseOcrProcessOptions>(dllHandle, "ReleaseOcrProcessOptions");

        return new OneOcrEngine(
            dllHandle, modelPath, createInit, setDelayLoad, createPipeline, createOpt, setMaxLine,
            runPipeline, getLineCount, getLine, getLineContent, getLineWordCount, getWord, getWordContent,
            getWordBoundingBox, getWordConfidence, releaseResult, releaseInit, releasePipeline, releaseOpt);
    }

    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_initialized) return;
        if (_initFailed)
            throw new OneOcrNativeException("native initialization previously failed");

        lock (_initLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_initialized) return;
            if (_initFailed) throw new OneOcrNativeException("native initialization previously failed");

            try
            {
                long res = _createInit(out _ctx);
                if (res != S_OK) throw new OneOcrNativeException($"CreateOcrInitOptions: 0x{res:X}");

                res = _setDelayLoad(_ctx, 0);
                if (res != S_OK) throw new OneOcrNativeException($"OcrInitOptionsSetUseModelDelayLoad: 0x{res:X}");

                var modelPathBytes = System.Text.Encoding.ASCII.GetBytes(_modelPath + "\0");
                var keyBytes = System.Text.Encoding.ASCII.GetBytes(ModelKey + "\0");

                unsafe
                {
                    fixed (byte* mp = modelPathBytes)
                    fixed (byte* kp = keyBytes)
                    {
                        res = _createPipeline((long)mp, (long)kp, _ctx, out _pipeline);
                    }
                }

                if (res != S_OK)
                    throw new OneOcrNativeException($"CreateOcrPipeline: 0x{res:X}");

                _initialized = true;
            }
            catch
            {
                _initFailed = true;
                throw;
            }
        }
    }

    public unsafe NativeWord[] Recognize(byte[] bgraPixels, int width, int height, int stride, double minConfidence = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureInitialized();

        long res = _createOpt(out long opt);
        if (res != S_OK) throw new OneOcrNativeException($"CreateOcrProcessOptions: 0x{res:X}");

        long instance = 0;
        try
        {
            res = _setMaxLine(opt, 1000);
            if (res != S_OK) throw new OneOcrNativeException($"OcrProcessOptionsSetMaxRecognitionLineCount: 0x{res:X}");

            var img = new Img
            {
                t = 3,
                col = width,
                row = height,
                _unk = 0,
                step = stride,
                data_ptr = 0
            };

            fixed (byte* p = bgraPixels)
            {
                img.data_ptr = (long)p;
                res = _runPipeline(_pipeline, ref img, opt, out instance);
            }

            if (res != S_OK) throw new OneOcrNativeException($"RunOcrPipeline: 0x{res:X}");
            if (instance == 0) return [];

            res = _getLineCount(instance, out long lineCount);
            if (res != S_OK || lineCount == 0) return [];

            var words = new List<NativeWord>();

            for (long li = 0; li < lineCount; li++)
            {
                res = _getLine(instance, li, out long line);
                if (res != S_OK || line == 0) continue;

                res = _getLineWordCount(line, out long wordCount);
                if (res != S_OK) continue;

                for (long wi = 0; wi < wordCount; wi++)
                {
                    res = _getWord(line, wi, out long word);
                    if (res != S_OK || word == 0) continue;

                    res = _getWordContent(word, out long contentPtr);
                    var text = Marshal.PtrToStringAnsi((nint)contentPtr) ?? "";

                    res = _getWordBoundingBox(word, out long boxPtr);
                    var bounds = ParseBoundingBox((nint)boxPtr);

                    double confidence = 1.0;
                    if (_getWordConfidence != null)
                    {
                        res = _getWordConfidence(word, out float conf);
                        if (res == S_OK)
                            confidence = conf;
                    }

                    words.Add(new NativeWord
                    {
                        Text = text,
                        Bounds = bounds,
                        Confidence = confidence
                    });
                }
            }

            return [.. words.Where(w => w.Confidence >= minConfidence)];
        }
        finally
        {
            if (instance != 0)
                _releaseResult?.Invoke(instance);
            _releaseOpt?.Invoke(opt);
        }
    }

    private static unsafe Rectangle ParseBoundingBox(nint ptr)
    {
        if (ptr == 0)
            return Rectangle.Empty;

        var data = (float*)ptr;
        float minX = MathF.Min(MathF.Min(data[0], data[2]), MathF.Min(data[4], data[6]));
        float minY = MathF.Min(MathF.Min(data[1], data[3]), MathF.Min(data[5], data[7]));
        float maxX = MathF.Max(MathF.Max(data[0], data[2]), MathF.Max(data[4], data[6]));
        float maxY = MathF.Max(MathF.Max(data[1], data[3]), MathF.Max(data[5], data[7]));

        return new Rectangle((int)MathF.Floor(minX), (int)MathF.Floor(minY), (int)MathF.Ceiling(MathF.Max(0, maxX - minX)), (int)MathF.Ceiling(MathF.Max(0, maxY - minY)));
    }

    private static string? FindOneOcrDll()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oneocr.dll");
            if (File.Exists(path)) return path;
        }
        catch { }

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

    private static string CopyToWritableLocation(string dllPath, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        var sourceDir = Path.GetDirectoryName(dllPath)!;

        var targetDll = Path.Combine(targetDir, "oneocr.dll");
        if (!File.Exists(targetDll) || File.GetLastWriteTime(targetDll) < File.GetLastWriteTime(dllPath))
            File.Copy(dllPath, targetDll, overwrite: true);

        CopyDep(sourceDir, targetDir, "onnxruntime.dll");
        CopyDep(sourceDir, targetDir, "onnxruntime_providers_shared.dll");
        CopyDep(sourceDir, targetDir, "oneocr.onemodel");

        return targetDll;
    }

    private static void CopyDep(string sourceDir, string targetDir, string fileName)
    {
        var src = Path.Combine(sourceDir, fileName);
        var dst = Path.Combine(targetDir, fileName);
        if (File.Exists(src) && (!File.Exists(dst) || File.GetLastWriteTime(dst) < File.GetLastWriteTime(src)))
            File.Copy(src, dst, overwrite: true);
    }

    private static string? FindModelFile(string dllDir)
    {
        var path = Path.Combine(dllDir, "oneocr.onemodel");
        if (File.Exists(path)) return path;

        var parent = Directory.GetParent(dllDir);
        if (parent is not null)
        {
            path = Path.Combine(parent.FullName, "oneocr.onemodel");
            if (File.Exists(path)) return path;
        }

        return null;
    }

    private static TDelegate? GetDelegate<TDelegate>(nint dllHandle, string name) where TDelegate : Delegate
    {
        var ptr = GetProcAddress(dllHandle, name);
        if (ptr == 0) return null;
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr);
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadLibraryEx(string lpFileName, nint hFile, uint dwFlags);

    private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

    [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern nint GetProcAddress(nint hModule, string lpProcName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool FreeLibrary(nint hModule);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_initLock)
        {
            if (_pipeline != 0)
            {
                _releasePipeline?.Invoke(_pipeline);
                _pipeline = 0;
            }

            if (_ctx != 0)
            {
                _releaseInit?.Invoke(_ctx);
                _ctx = 0;
            }

            _initialized = false;
        }

        if (_dllHandle != 0)
        {
            FreeLibrary(_dllHandle);
            _dllHandle = 0;
        }
    }
}
