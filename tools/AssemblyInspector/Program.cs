using System.Reflection;
using System.Runtime.Loader;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: AssemblyInspector <dll-or-pkt> [type-filter ...]");
    return 2;
}

var target = Path.GetFullPath(args[0]);
var filterTerms = args.Skip(1).ToArray();

var workingDir = Path.Combine(Path.GetTempPath(), "w2-assembly-inspector-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(workingDir);

try
{
    if (Path.GetExtension(target).Equals(".pkt", StringComparison.OrdinalIgnoreCase))
    {
        System.IO.Compression.ZipFile.ExtractToDirectory(target, workingDir);
        target = Directory.GetFiles(workingDir, "*.dll").First();
    }

    var searchDirs = new[]
    {
        Path.GetDirectoryName(target)!,
        @"C:\Program Files\Autodesk\AutoCAD 2026",
        @"C:\Program Files\Autodesk\AutoCAD 2026\C3D",
        @"C:\Program Files\Autodesk\AutoCAD 2026\C3D\SACRuntime",
        Path.GetDirectoryName(typeof(object).Assembly.Location)!,
        Path.GetDirectoryName(typeof(System.Windows.Forms.Form).Assembly.Location)!,
    };

    AssemblyLoadContext.Default.Resolving += (_, name) =>
    {
        foreach (var dir in searchDirs)
        {
            var candidate = Path.Combine(dir, name.Name + ".dll");
            if (File.Exists(candidate))
            {
                try
                {
                    return AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate);
                }
                catch
                {
                    // Mixed-mode AutoCAD assemblies cannot always be loaded outside Civil 3D.
                }
            }
        }

        return null;
    };

    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(target);

    Console.WriteLine("Assembly: " + assembly.FullName);
    Console.WriteLine("File: " + target);
    Console.WriteLine();
    Console.WriteLine("References:");
    foreach (var reference in assembly.GetReferencedAssemblies().OrderBy(r => r.Name))
    {
        Console.WriteLine($"  {reference.Name} {reference.Version}");
    }

    Console.WriteLine();
    Console.WriteLine("Types:");
    foreach (var type in SafeGetTypes(assembly).OrderBy(t => t.FullName))
    {
        if (filterTerms.Length > 0 &&
            !filterTerms.Any(term => (type.FullName ?? type.Name).Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            continue;
        }

        Console.WriteLine("TYPE " + type.FullName);
        Console.WriteLine("  Base: " + type.BaseType?.FullName);

        foreach (var ctor in SafeGetConstructors(type))
        {
            Console.WriteLine($"  CTOR {FormatSignature(type.Name, ctor)}");
        }

        foreach (var property in SafeGetProperties(type).OrderBy(p => p.Name))
        {
            Console.WriteLine($"  PROP {SafeTypeName(property.PropertyType)} {property.Name}");
        }

        foreach (var method in SafeGetMethods(type).OrderBy(m => m.Name))
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            Console.WriteLine($"  METHOD {SafeTypeName(method.ReturnType)} {FormatSignature(method.Name, method)}");
        }
    }
}
finally
{
    try
    {
        Directory.Delete(workingDir, true);
    }
    catch
    {
    }
}

return 0;

static IEnumerable<Type> SafeGetTypes(Assembly assembly)
{
    try
    {
        return assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
        foreach (var loaderException in ex.LoaderExceptions)
        {
            Console.WriteLine("LOADER: " + loaderException?.Message);
        }

        return ex.Types.Where(t => t != null)!;
    }
}

static IEnumerable<ConstructorInfo> SafeGetConstructors(Type type)
{
    try
    {
        return type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }
    catch
    {
        return Array.Empty<ConstructorInfo>();
    }
}

static IEnumerable<PropertyInfo> SafeGetProperties(Type type)
{
    try
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
    }
    catch
    {
        return Array.Empty<PropertyInfo>();
    }
}

static IEnumerable<MethodInfo> SafeGetMethods(Type type)
{
    try
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
    }
    catch
    {
        return Array.Empty<MethodInfo>();
    }
}

static string FormatSignature(string name, MethodBase method)
{
    try
    {
        var parameters = string.Join(", ", method.GetParameters().Select(p => $"{SafeTypeName(p.ParameterType)} {p.Name}"));
        return $"{name}({parameters})";
    }
    catch (Exception ex)
    {
        return $"{name}(<parameters unavailable: {ex.GetType().Name}>)";
    }
}

static string SafeTypeName(Type? type)
{
    if (type == null)
    {
        return "<none>";
    }

    try
    {
        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var genericName = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tick = genericName.IndexOf('`');
        if (tick >= 0)
        {
            genericName = genericName[..tick];
        }

        return $"{genericName}<{string.Join(", ", type.GetGenericArguments().Select(SafeTypeName))}>";
    }
    catch
    {
        return type.Name;
    }
}
