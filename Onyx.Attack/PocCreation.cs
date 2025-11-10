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
    public static void Create(string udcTargetPath, List<string> dependencyPaths, string outputPath, bool obfuscate = false)
    {
        PostCompilation.Merge(udcTargetPath, dependencyPaths, outputPath, obfuscate);
    }
}