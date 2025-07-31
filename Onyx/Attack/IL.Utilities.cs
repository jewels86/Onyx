using System.Reflection;
using System.Runtime.Loader;

namespace Onyx.Attack;

public static partial class IL
{
    public class TempContext : AssemblyLoadContext
    {
        public WeakReference<TempContext> WeakRef { get; set; }

        public TempContext() : base(isCollectible: true)
        {
            WeakRef = new(this);
        }

        public Assembly FromStream(Stream stream) => LoadFromStream(stream);
        public bool IsAlive => WeakRef.TryGetTarget(out _);
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
}