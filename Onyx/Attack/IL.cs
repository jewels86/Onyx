using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Onyx.Shared;
using static Onyx.Attack.Reflection;
using static Onyx.Shared.GeneralUtilities;
using static Onyx.Attack.ClassBuilder;

namespace Onyx.Attack;

public static partial class IL
{
    #region Quick Evaluation
    public static Func<Task<object?>> Create(string code, ScriptOptions? options = null)
    {
        options ??= ScriptOptions.Default;
        options = options.AddImports(StandardImports)
            .AddReferences(typeof(object).Assembly);
        var result = CSharpScript.Create(code, options).CreateDelegate();
        return async () => await result.Invoke();
    } // you can remove the memory references so we can do disk based
    
    public static Func<Task<object?>> Create(string code, object globals, ScriptOptions? options = null)
    {
        options ??= ScriptOptions.Default;
        options = options.AddReferences(typeof(object).Assembly)
            .AddReferences(SafeReferenceFromAssembly(globals.GetType().Assembly))
            .AddImports(StandardImports);
        
        var result = CSharpScript.Create(code, options, globals.GetType()).CreateDelegate();
        return async () => await result.Invoke(globals);
    } // roslyn is loading the assembly from the reference but using a different context, so casting errors come up
    
    public static async Task<object?> Run(string code, ScriptOptions? options = null)
    {
        options ??= ScriptOptions.Default;
        options = options.AddImports(StandardImports)
            .AddReferences(typeof(object).Assembly);
        var script = CSharpScript.Create(code, options);
        var runner = script.CreateDelegate();
        return await runner.Invoke();
    }
    
    public static async Task<object?> Run(string code, object globals, ScriptOptions? options = null, MetadataReference? mr = null)
    {
        options ??= ScriptOptions.Default;
        options = options.AddImports(StandardImports)
            .AddReferences(typeof(object).Assembly)
            .AddReferences(SafeReferenceFromAssembly(globals.GetType().Assembly));
        var script = CSharpScript.Create(code, options, globals.GetType());
        var runner = script.CreateDelegate();
        return await runner.Invoke(globals);
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
        string path = Path.Combine(Path.GetTempPath(), $"globals-{assemblyName}.dll");

        var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
        var result = compilation.Emit(stream);
        
        if (!result.Success) throw new UnableToCompileException("Couldn't compile the code: " + FromStrings(result.Diagnostics.Select(x => x.ToString()), "\n"));

        stream.Flush();
        stream.Dispose();
        
        var assembly = tctx.FromPath(path);
        tctx.AddedAssemblies.Add(path);
        
        return (assembly, tctx);
    }
    
    public static Type FromAssembly(Assembly assembly, string typeName)
    {
        return assembly.GetType(typeName) ??
               throw new UnableToCompileException($"Type '{typeName}' not found in assembly.");
    }
    #endregion
    #region In Context Compilation (ICC)
    public static (Type, TempContext) ICCGlobalsType(List<Expression<Func<object>>> context, TempContext? tctx = null, string? before = null, string? typeName = null)
    {
        List<VariablePackage> variables = context.Select(o => FromObject(o)).ToList();
        if (typeName == null) 
            typeName = $"Globals_{RemoveIllegalCharacters(NewGUID(8))}";
        StringBuilder sb = new();
        sb.AppendLine(StandardUsings);
        sb.AppendLine(before ?? "");

        sb.AppendLine("public class " + typeName);
        sb.AppendLine("{");

        foreach (var variable in variables)
        {
            sb.AppendLine($"    public {GetCSharpTypeName(variable.Type)} {variable.Name} {{ get; set; }} = default!;");
        }
        
        sb.AppendLine();
        sb.Append($"    public {typeName}(");
        foreach (var variable in variables)
        {
            sb.Append($"{GetCSharpTypeName(variable.Type)} {variable.Name}2");
            if (variable != variables.Last())
                sb.Append(",");
        }
        sb.AppendLine(")");
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

    public static (Func<Task<object?>>, TempContext) ICC(string code, List<Expression<Func<object>>> context, TempContext? tctx = null, ScriptOptions? options = null)
    {
        bool usedTempContext = tctx != null;
        var (globalsType, ntctx) = ICCGlobalsType(context, tctx);
        var globals = New(globalsType, context.Select(x => x.Compile()()).ToArray());
        if (globals == null) throw new UnableToCompileException("Failed to create globals instance.");
        return (ICC(code, globals, options), ntctx);
    }

    public static Func<Task<object?>> ICC(string code, object globals, ScriptOptions? options = null)
    {
        return Create(code, globals, options);
    }

    #endregion
}