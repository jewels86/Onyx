using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using static Onyx.Shared.GeneralUtilities;
using MethodDefinition = Mono.Cecil.MethodDefinition;

namespace Onyx.Shared;

public partial class DynamicTypeBuilder
{
    public string Name { get; set; }
    public TypeKind Kind { get; set; }
    public AccessModifier Access { get; set; } = AccessModifier.Public;
    
    public List<Type> Interfaces { get; set; } = [];
    public Type? BaseType { get; set; } = null;
    
    public List<Reflection.FieldPackage> Fields { get; set; } = [];
    public List<Reflection.PropertyPackage> Properties { get; set; } = [];
    public List<string> RawMembers { get; set; } = [];

    public DynamicTypeBuilder(string name, TypeKind kind = TypeKind.Class)
    {
        Name = name;
        Kind = kind;
    }
    
    public void AddField(string name, Type type, AccessModifier access = AccessModifier.Public, object? defaultValue = null)
    {
        var field = new Reflection.FieldPackage(name, type, defaultValue, access);
        Fields.Add(field);
    }
    
    public void AddLazyProperty(string name, Type type, AccessModifier access = AccessModifier.Public, object? defaultValue = null)
    {
        var property = new Reflection.PropertyPackage(name, type, defaultValue, access);
        Properties.Add(property);
    }

    public void AddInterface(Type type)
    {
        Interfaces.Add(type);
    }
    
    public void SetBaseType(Type type)
    {
        BaseType = type;
    }
    
    public void AddRawMember(string member)
    {
        RawMembers.Add(member);
    }

    public (Type, Compilation.TempContext) Build()
    {
        StringBuilder sb = new();
        sb.AppendLine(Compilation.StandardUsings);
        sb.Append($"{AccessModifierToString(Access)} {TypeKindToString(Kind)} {Name}");
        if (BaseType != null)
            sb.Append($" : {GetCSharpTypeName(BaseType)}");
        foreach (var iface in Interfaces) 
            sb.Append($", {GetCSharpTypeName(iface)}");
        sb.AppendLine(" {");
        foreach (var field in Fields)
        {
            sb.Append($"    {AccessModifierToString(field.Access)} {GetCSharpTypeName(field.Type)} {field.Name};");
        }
        foreach (var property in Properties)
        {
            sb.AppendLine($"    {AccessModifierToString(property.Access)} {GetCSharpTypeName(property.Type)} {property.Name} {{ get; set; }}");
        }

        foreach (string raw in RawMembers)
        {
            sb.AppendLine("    " + raw);
        }

        sb.AppendLine("}");

        var (asm, tctx) = Compilation.Compile(sb.ToString());
        Type type = asm.GetType(Name) ??
                    throw new Compilation.UnableToCompileException($"Type '{Name}' not found in compiled assembly.");
        return (type, tctx);
    }
}