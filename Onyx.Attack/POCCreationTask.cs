using System.Reflection;
using HarmonyLib.Tools;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Onyx.Shared;
using ManifestResourceAttributes = Mono.Cecil.ManifestResourceAttributes;

namespace Onyx.Attack;

/// <summary>
/// Create a Packaged Onyx Compilation (POC) by embedding dependencies as resources and adding a bootstrapper to load them at runtime.
/// </summary>
public class POCCreationTask : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// The path to the assembly that was just compiled.
    /// </summary>
    [Required]
    public string UdcTargetPath { get; set; } = string.Empty;
    
    /// <summary>
    /// The path to the directory containing the dependencies.
    /// </summary>
    [Required]
    public string DependenciesDirectory { get; set; } = string.Empty;
    
    /// <summary>
    /// The path where the output assembly should be written.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.Normal, "Onyx is creating the POC...");
            var asmDef = AssemblyDefinition.ReadAssembly(UdcTargetPath);
            List<string> dependencies = Directory.GetFiles(DependenciesDirectory)
                .Where(x => x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var dependency in dependencies)
            {
                byte[] dependencyBytes = File.ReadAllBytes(dependency);
                asmDef.MainModule.Resources.Add(
                    new EmbeddedResource(Path.GetFileName(dependency), ManifestResourceAttributes.Public, dependencyBytes));
            }
            
            asmDef.MainModule.AssemblyReferences.Clear();

            string bootstrapper = @"
            using System;
            using System.Reflection;
            using System.Runtime.CompilerServices;
            using System.Linq;

            public static class Bootstrapper
            {
                [ModuleInitializer]
                public static void Init()
                {
                    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                    {
                        foreach (var resName in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.EndsWith("".dll"", StringComparison.OrdinalIgnoreCase)))
                        {
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName))
                            {
                                if (stream == null) return null;
                                byte[] assemblyData = new byte[stream.Length];
                                stream.Read(assemblyData, 0, assemblyData.Length);
                                var assembly = Assembly.Load(assemblyData);
                            }
                        }
                    };
                }
            }";
            Compilation.CompileAndUse(bootstrapper, a =>
            {
                var bootstrapAsmDef = AssemblyDefinition.ReadAssembly(a.Location);
                var bootstrapTypeDef = bootstrapAsmDef.MainModule.GetType("Bootstrapper");
                PostCompilation.ImportType(bootstrapTypeDef, asmDef.MainModule);
                asmDef.Write(OutputPath);
                asmDef = null!;
                
                return null;
            });
            
            Log.LogMessage(MessageImportance.High, $"Onyx has created the POC at: {OutputPath}");

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }
}