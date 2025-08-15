using HarmonyLib.Tools;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Onyx.Attack;

public class PostCompileTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string TargetPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.Normal, "Obfuscating all types and members with attribute [ObfuscateAttribute] in {0}", TargetPath);
            var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(TargetPath);
            Shared.PostCompilation.ObfuscateWithAttribute(assembly);
            
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            Log.LogMessage(MessageImportance.High, "Failed to obfuscate assembly: {0}", TargetPath);
            return false;
        }
    }
}