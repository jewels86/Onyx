using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using static Onyx.Attack.Reflection;
using static Onyx.Shared.GeneralUtilities;
using static Onyx.Attack.ClassBuilder;

namespace Onyx.Attack;

public static partial class IL
{
    #region Quick Evaluation
    public static ScriptRunner<object> Create(string code, ScriptOptions? options = null)
    {
        return CSharpScript.Create(code).CreateDelegate();
    }
    
    public static ScriptRunner<object> Create(string code, object globals, ScriptOptions? options = null)
    {
        return CSharpScript.Create(code, options).CreateDelegate();
    }
    
    public static object Run(string code, ScriptOptions? options = null)
    {
        var script = CSharpScript.Create(code, options);
        var runner = script.CreateDelegate();
        return runner().Result;
    }
    
    public static object Run(string code, object globals, ScriptOptions? options = null)
    {
        var script = CSharpScript.Create(code, options, globals.GetType());
        var runner = script.CreateDelegate();
        return runner(globals).Result;
    }
    #endregion
    #region Compliation
    public static (Assembly, TempContext) Compile(string code, string assemblyName, TempContext? tctx = null)
    {
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (tpa is null) throw new Exception("Could not get trusted platform assemblies");

        var references = tpa.Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(assemblyName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(tree)
            .AddReferences(references);

        tctx = tctx ?? new TempContext();
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        ms.Seek(0, SeekOrigin.Begin);
        if (!result.Success) throw new UnableToCompileException("Couldn't compile the code: " + FromStrings(result.Diagnostics.Select(x => x.ToString()), "\n"));
        return (tctx.LoadFromStream(ms), tctx);
    }
    
    public static Type FromAssembly(Assembly assembly, string typeName)
    {
        return assembly.GetType(typeName) ??
               throw new UnableToCompileException($"Type '{typeName}' not found in assembly.");
    }
    #endregion
    #region In Context Compilation (ICC)

    public static (Type, TempContext) ICCGlobalsType(List<(string, object)> context, TempContext? tctx = null, string? typeName = null)
    {
        List<VariablePackage> variables = context.Select(t => FromObject(t.Item2)).ToList();
        if (typeName == null) 
            typeName = $"Globals_{RemoveIllegalCharacters(NewGUID(8))}";
        StringBuilder sb = new();

        sb.AppendLine("public class " + typeName);
        sb.AppendLine("{");

        foreach (var variable in variables)
        {
            sb.AppendLine($"    public {variable.Type.Name} {variable.Name} {{ get; set; }} = default!;");
        }
        
        sb.AppendLine();
        sb.AppendLine($"    public {typeName}(");
        foreach (var variable in variables)
        {
            sb.AppendLine($"        {variable.Type.Name} {variable.Name}2");
            if (variable != variables.Last())
                sb.Append(",");
            else
                sb.AppendLine();
        }
        sb.AppendLine("    )");
        sb.AppendLine("    {");
        foreach (var variable in variables)
        {
            sb.AppendLine($"        {variable.Name} = {variable.Name.ToLower()}2;");
        }
        sb.AppendLine("    }");

        sb.AppendLine("}");
        
        Console.WriteLine(sb.ToString());
        var (assembly, newTempContext) = Compile(sb.ToString(), $"globals_{NewGUID(8)}", tctx);
        if (assembly == null)
            throw new UnableToCompileException("Failed to compile globals type.");
        
        
        return (FromAssembly(assembly, typeName), newTempContext);
    }

    public static ScriptRunner<Object> ICC(string code, List<(string, object)> context, TempContext? tctx = null, ScriptOptions? options = null)
    {
        bool usedTempContext = tctx != null;
        var (globalsType, ntctx) = ICCGlobalsType(context, tctx);
        var globals = New(globalsType, context.Select(x => x.Item2).ToArray());
        if (globals == null) throw new UnableToCompileException("Failed to create globals instance.");
        if (!usedTempContext) ntctx.Unload();
        return ICC(code, globals, options);
    }

    public static ScriptRunner<object> ICC(string code, object globals, ScriptOptions? options = null)
    {
        return Create(code, globals, options);
    }
    #endregion
}