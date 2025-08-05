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
using static Onyx.Attack.LegacyClassBuilder;
using Mono.Cecil;

namespace Onyx.Attack;

public static partial class Compilation
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
    
    public static Func<Task<object?>> Create(string code, object globals, IEnumerable<MetadataReference>? extraReferences = null, 
        IEnumerable<string>? extraImports = null, ScriptOptions? options = null)
    { // Refactor this whole this to use a dictionary 
        options ??= ScriptOptions.Default;
        options = options.AddReferences(typeof(object).Assembly)
            .AddReferences(SafeReferenceFromAssembly(globals.GetType().Assembly))
            .AddReferences(extraReferences ?? [])
            .AddImports(extraImports ?? [])
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
    public static void Compile(string code, Assembly? targetAssembly = null, CSharpCompilationOptions? options = null)
    {
        var assemblyName = NewGUID(8, true);
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (tpa is null) throw new Exception("Could not get trusted platform assemblies");
        if (targetAssembly == null) targetAssembly = Assembly.GetExecutingAssembly();
        var targetAssemblyDefinition = PostCompilation.GetDefinitionFrom(targetAssembly);
        if (targetAssemblyDefinition == null) 
            throw new UnableToCompileException("Target assembly definition could not be retrieved.");

        var references = tpa.Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Concat(PostCompilation.ExtractReferences(Assembly.GetExecutingAssembly()))
            .Concat(PostCompilation.ExtractReferences(targetAssembly))
            .Distinct()
            .ToList();
        

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(assemblyName)
            .WithOptions(options ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(tree)
            .AddReferences(references);

        using var peStream = new MemoryStream();
        var result = compilation.Emit(peStream);
        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .ToList();
            throw new UnableToCompileException($"Compilation failed: {string.Join(", ", errors)}");
        }
        
        peStream.Seek(0, SeekOrigin.Begin);
        PostCompilation.AsmInject(targetAssemblyDefinition, peStream);
        targetAssemblyDefinition.Write(targetAssembly.Location);
    }
    
    public static Type FromAssembly(Assembly assembly, string typeName)
    {
        return assembly.GetType(typeName) ??
               throw new UnableToCompileException($"Type '{typeName}' not found in assembly.");
    }
    #endregion
    #region In Context Compilation (ICC)
    public static Dictionary<string, object?> ICCGlobals(List<Expression<Func<object>>> context)
    {
        List<VariablePackage> variables = context.Select(o => FromObject(o)).ToList();
        var globals = variables.Select(x => (x.Name, x.Value)).ToDictionary();
        return globals;
    }

    public static Func<Task<object?>> ICC(string code, List<Expression<Func<object>>> context, ScriptOptions? options = null)
    {
        var globals = ICCGlobals(context);
        return ICC(code, globals, options);
    }

    public static Func<Task<object?>> ICC(string code, Dictionary<string, object?> globals, ScriptOptions? options = null)
    {
        StringBuilder sb = new();
        List<MetadataReference> extraReferences = [];
        foreach (var (name, value) in globals)
        {
            Type type = value?.GetType() ?? typeof(object);
            string typeName = GetCSharpTypeName(type);
            sb.AppendLine($"{typeName} {name} = ({typeName})Globals[\"{name}\"];");
            extraReferences.Add(SafeReferenceFromAssembly(type.Assembly));
        }

        sb.AppendLine();
        sb.Append(code);
        code = sb.ToString();
        return Create(code, new GlobalsType(globals), extraReferences, options: options);
    }

    #endregion
}