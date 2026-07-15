using System.IO;
using System.Reflection;

namespace Zaya.OCR.Services;

/// <summary>
/// Discovers available OCR engines by scanning loaded assemblies
/// for concrete implementations of <see cref="IOCRService"/>.
/// Traverses all referenced assemblies recursively so that
/// implementations in referenced NuGet/project packages are discovered.
/// </summary>
public sealed class OcrEngineProvider
{
    private readonly List<OcrEngineInfo> _engines;

    /// <summary>
    /// Initializes a new instance and scans the current <see cref="AppDomain"/>
    /// for available OCR engines.
    /// </summary>
    /// <param name="extraAssemblies">Optional additional assemblies to scan.</param>
    public OcrEngineProvider(IEnumerable<Assembly>? extraAssemblies = null)
    {
        var log = @"D:\p\Zaya\debug.txt";
        var assemblies = GetAllAssemblies()
            .Concat(extraAssemblies ?? [])
            .DistinctBy(a => a.FullName);

        File.AppendAllText(log, $"[{DateTime.Now:T}] OcrEngineProvider: scanning {assemblies.Count()} assemblies\n");

        var candidates = assemblies
            .SelectMany(a => SafeGetTypes(a))
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(t => t.GetInterfaces().Contains(typeof(IOCRService)))
            .ToList();

        File.AppendAllText(log, $"  candidates: {candidates.Count}\n");

        foreach (var c in candidates)
        {
            try
            {
                var ok = Activator.CreateInstance(c) is IOCRService svc && svc.IsAvailable;
                File.AppendAllText(log, $"  {c.FullName}: IsAvailable={ok}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(log, $"  {c.FullName}: ERROR {ex.GetType().Name}: {ex.Message}\n");
            }
        }

        _engines = candidates
            .Select(t => CreateEngine(t))
            .Where(e => e is not null)
            .Select(e => e!)
            .Where(e =>
            {
                try { return e.IsAvailable; }
                catch { return false; }
            })
            .Select(e => new OcrEngineInfo(e.EngineId, e))
            .ToList();
    }

    /// <summary>
    /// Gets the list of all available OCR engines on this system.
    /// </summary>
    public IReadOnlyList<OcrEngineInfo> AvailableEngines => _engines;

    /// <summary>
    /// Finds an engine by its <see cref="IOCRService.EngineId"/>.
    /// Returns <c>null</c> if not found or not available.
    /// </summary>
    public OcrEngineInfo? GetById(string engineId)
    {
        return _engines.FirstOrDefault(e =>
            string.Equals(e.Id, engineId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the first available engine, or <c>null</c> if none are available.
    /// </summary>
    public OcrEngineInfo? GetFirstAvailable()
    {
        return _engines.FirstOrDefault();
    }

    private static IEnumerable<Assembly> GetAllAssemblies()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<Assembly>(AppDomain.CurrentDomain.GetAssemblies());

        while (stack.Count > 0)
        {
            var asm = stack.Pop();
            if (!seen.Add(asm.FullName ?? asm.GetName().Name ?? ""))
                continue;

            yield return asm;

            foreach (var refName in asm.GetReferencedAssemblies())
            {
                try
                {
                    var refAsm = Assembly.Load(refName);
                    if (!seen.Contains(refAsm.FullName ?? refName.Name ?? ""))
                        stack.Push(refAsm);
                }
                catch
                {
                    // skip assemblies that cannot be loaded
                }
            }
        }
    }

    private static IOCRService? CreateEngine(Type type)
    {
        try
        {
            return Activator.CreateInstance(type) as IOCRService;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
        catch
        {
            return [];
        }
    }
}

/// <summary>
/// Describes an available OCR engine.
/// </summary>
/// <param name="Id">Unique engine identifier, matches <see cref="IOCRService.EngineId"/>.</param>
/// <param name="Service">The engine instance.</param>
public sealed record OcrEngineInfo(
    string Id,
    IOCRService Service);
