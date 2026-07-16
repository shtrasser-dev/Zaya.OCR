using System.Reflection;

namespace Zaya.OCR.Services;

public sealed class OcrEngineProvider : IDisposable
{
    private readonly List<OcrEngineInfo> _engines;

    public OcrEngineProvider(IEnumerable<string>? pluginDirectories = null)
    {
        var assemblies = GetAllAssemblies().ToList();
        if (pluginDirectories is not null)
        {
            foreach (var dir in pluginDirectories)
            {
                try
                {
                    foreach (var dll in Directory.GetFiles(dir, "*.dll"))
                    {
                        try { assemblies.Add(Assembly.LoadFrom(dll)); }
                        catch { }
                    }
                }
                catch { }
            }
        }

        _engines = assemblies
            .DistinctBy(a => a.FullName)
            .SelectMany(a => SafeGetTypes(a))
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(t => t.GetInterfaces().Contains(typeof(IOCRService)))
            .Select(t => CreateEngine(t))
            .Where(e => e is not null)
            .Select(e => e!)
            .Select(e => new OcrEngineInfo(e.EngineId, e))
            .ToList();
    }

    public IReadOnlyList<OcrEngineInfo> AvailableEngines => _engines;
    public OcrEngineInfo? GetById(string engineId) => _engines.FirstOrDefault(e => string.Equals(e.Id, engineId, StringComparison.OrdinalIgnoreCase));
    public OcrEngineInfo? GetFirstAvailable() => _engines.FirstOrDefault();

    public void Dispose()
    {
        foreach (var e in _engines)
            e.Service.Dispose();
    }

    private static IEnumerable<Assembly> GetAllAssemblies()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        while (stack.Count > 0)
        {
            var asm = stack.Pop();
            if (!seen.Add(asm.FullName ?? asm.GetName().Name ?? "")) continue;
            yield return asm;
            foreach (var refName in asm.GetReferencedAssemblies())
            {
                try
                {
                    var refAsm = Assembly.Load(refName);
                    if (!seen.Contains(refAsm.FullName ?? refName.Name ?? ""))
                        stack.Push(refAsm);
                }
                catch { }
            }
        }
    }

    private static IOCRService? CreateEngine(Type type) { try { return Activator.CreateInstance(type) as IOCRService; } catch { return null; } }
    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
        catch { return []; }
    }
}

public sealed record OcrEngineInfo(string Id, IOCRService Service);
