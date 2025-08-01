using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Onyx.Attack;

public static partial class IL
{
    public class TempContext : AssemblyLoadContext
    {
        public WeakReference<TempContext> WeakRef { get; set; }
        public List<string> AddedAssemblies { get; } = [];

        public TempContext() : base(isCollectible: true)
        {
            WeakRef = new(this);
        }

        public Assembly FromPath(string path) => LoadFromAssemblyPath(path);
        public bool IsAlive => WeakRef.TryGetTarget(out _);

        public void FullUnload()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Unload();
            foreach (var assembly in AddedAssemblies)
            {
                try { File.Delete(assembly); }
                catch (IOException) { }
            }
        }
    }
    
    public static string StandardUsings =>
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Text;
        using System.Threading.Tasks;
        using System.Reflection;
        """;

    public static IEnumerable<string> StandardImports =>
    [
        "System",
        "System.Collections.Generic",
        "System.Text",
        "System.Threading.Tasks",
        "System.Reflection"
    ];
    
    public class UnableToCompileException : Exception
    {
        public UnableToCompileException(string message) : base(message) { }
        public UnableToCompileException(string message, Exception inner) : base(message, inner) { }
    }
    
    public static MetadataReference SafeReferenceFromAssembly(Assembly assembly)
    {
        if (string.IsNullOrEmpty(assembly.Location))
            throw new ArgumentException($"Assembly '{assembly.FullName}' has no location and cannot be referenced.");
        return MetadataReference.CreateFromFile(assembly.Location);
    }

}