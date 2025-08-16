using HarmonyLib.Tools;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Onyx.Attack;

public class PostCompileTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string TargetPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.Normal, "Obfuscating all types and members with attribute [ObfuscateAttribute] in {0}...", TargetPath);
            var assembly = AssemblyDefinition.ReadAssembly(TargetPath);
            Shared.PostCompilation.ObfuscateWithAttribute(assembly);
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            Log.LogMessage(MessageImportance.High, "Failed to obfuscate assembly: {0}", TargetPath);
            return false;
        }

        try
        {
            Log.LogMessage(MessageImportance.Normal, "Obfuscating all types and members in Onyx.Attack...");
            var assembly = AssemblyDefinition.ReadAssembly(TargetPath);
            List<TypeDefinition> typesToObfuscate = [];
            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    if (type.Namespace.StartsWith("Onyx"))
                        typesToObfuscate.Add(type);
                }
            }
            List<IMemberDefinition> members = typesToObfuscate.Cast<IMemberDefinition>().ToList();
            Shared.PostCompilation.Obfuscate(members);
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            Log.LogMessage(MessageImportance.High, "Failed to obfuscate Onyx.Attack assembly: {0}", TargetPath);
            return false;
        }

        return true;
    }
}