using System.Reflection;
using System.Text;

namespace Onyx.Attack;

public partial class DynamicTypeBuilder
{
    [Flags]
    public enum TypeKind
    {
        Class = 2, 
        Struct = 4, 
        Interface = 8, 
        Enum = 16, 
        Record = 32, 
        Static = 64, 
        Abstract = 128, 
        Delegate = 256
    }

    public static string TypeKindToString(TypeKind kind)
    {
        StringBuilder sb = new();
        if (kind.HasFlag(TypeKind.Static)) sb.Append("static ");
        if (kind.HasFlag(TypeKind.Abstract)) sb.Append("abstract ");
        if (kind.HasFlag(TypeKind.Class)) sb.Append("class ");
        else if (kind.HasFlag(TypeKind.Struct)) sb.Append("struct ");
        else if (kind.HasFlag(TypeKind.Interface)) sb.Append("interface ");
        else if (kind.HasFlag(TypeKind.Enum)) sb.Append("enum ");
        else if (kind.HasFlag(TypeKind.Record)) sb.Append("record ");
        else if (kind.HasFlag(TypeKind.Delegate)) sb.Append("delegate ");
        return sb.ToString().Trim();
    }
    
    public static string AccessModifierToString(AccessModifier access)
    {
        StringBuilder sb = new();
        if (access.HasFlag(AccessModifier.Public)) sb.Append("public ");
        if (access.HasFlag(AccessModifier.Private)) sb.Append("private ");
        if (access.HasFlag(AccessModifier.Protected)) sb.Append("protected ");
        if (access.HasFlag(AccessModifier.Internal)) sb.Append("internal ");
        if (access.HasFlag(AccessModifier.ProtectedInternal)) sb.Append("protected internal ");
        if (access.HasFlag(AccessModifier.PrivateProtected)) sb.Append("private protected ");
        if (access.HasFlag(AccessModifier.Sealed)) sb.Append("sealed ");
        if (access.HasFlag(AccessModifier.Static)) sb.Append("static ");
        if (access.HasFlag(AccessModifier.Abstract)) sb.Append("abstract ");
        if (access.HasFlag(AccessModifier.Virtual)) sb.Append("virtual ");
        if (access.HasFlag(AccessModifier.Override)) sb.Append("override ");
        return sb.ToString().Trim();
    }

    public static Mono.Cecil.MethodAttributes CecilMethodAttributes(MethodAttributes attributes)
    {
        Mono.Cecil.MethodAttributes cecilAttributes;
        if (attributes.HasFlag(MethodAttributes.Public)) cecilAttributes = Mono.Cecil.MethodAttributes.Public;
        else if (attributes.HasFlag(MethodAttributes.Private)) cecilAttributes = Mono.Cecil.MethodAttributes.Private;
        else if (attributes.HasFlag(MethodAttributes.Family)) cecilAttributes = Mono.Cecil.MethodAttributes.Family;
        else if (attributes.HasFlag(MethodAttributes.Assembly)) cecilAttributes = Mono.Cecil.MethodAttributes.Assembly;
        else if (attributes.HasFlag(MethodAttributes.FamORAssem)) cecilAttributes = Mono.Cecil.MethodAttributes.FamORAssem;
        else if (attributes.HasFlag(MethodAttributes.FamANDAssem))
            cecilAttributes = Mono.Cecil.MethodAttributes.FamANDAssem;
        else cecilAttributes = Mono.Cecil.MethodAttributes.Private;
        if (attributes.HasFlag(MethodAttributes.Static)) cecilAttributes |= Mono.Cecil.MethodAttributes.Static;
        if (attributes.HasFlag(MethodAttributes.Abstract)) cecilAttributes |= Mono.Cecil.MethodAttributes.Abstract;
        if (attributes.HasFlag(MethodAttributes.Virtual)) cecilAttributes |= Mono.Cecil.MethodAttributes.Virtual;
        if (attributes.HasFlag(MethodAttributes.NewSlot)) cecilAttributes |= Mono.Cecil.MethodAttributes.NewSlot;
        if (attributes.HasFlag(MethodAttributes.Final)) cecilAttributes |= Mono.Cecil.MethodAttributes.Final;

        return cecilAttributes;
    }
}