using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using static Onyx.Shared.GeneralUtilities;
using MethodDefinition = Mono.Cecil.MethodDefinition;

namespace Onyx.Attack;

public partial class DynamicTypeBuilder
{
    public string Name { get; set; }
    public TypeKind Kind { get; set; }
    public AccessModifier Access { get; set; } = AccessModifier.Public;
    
    public List<Type> Interfaces { get; set; } = [];
    public Type? BaseType { get; set; } = null;
    
    public List<Reflection.MethodPackage> Methods { get; set; } = [];
    public List<Reflection.FieldPackage> Fields { get; set; } = [];
    public List<Reflection.PropertyPackage> Properties { get; set; } = [];
    public List<string> RawMembers { get; set; } = [];

    public DynamicTypeBuilder(string name, TypeKind kind = TypeKind.Class)
    {
        Name = name;
        Kind = kind;
    }

    public void AddMethod(Delegate function, string name, AccessModifier access)
    {
        var method = new Reflection.MethodPackage(function.GetMethodInfo());
        method.Name = name;
        method.Access = access;
        method.CompiledDelegate = function;
        Methods.Add(method);
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

    public Type Build()
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

        foreach (var method in Methods)
        {
            if (method.Name.Contains("operator"))
                sb.AppendLine($"    public static {method.Name}({string.Join(", ", method.Parameters.Select(p => $"{GetCSharpTypeName(p)} {p.Name}"))})");
            else
                sb.AppendLine($"    {AccessModifierToString(method.Access)} {GetCSharpTypeName(method.ReturnType)} {method.Name}({string.Join(", ", method.Parameters.Select(p => $"{GetCSharpTypeName(p.Type)} {p.Name}"))})");
            sb.AppendLine($"    {{ throw new NotImplementedException(); }}");
        }

        foreach (string raw in RawMembers)
        {
            sb.AppendLine("    " + raw);
        }

        sb.AppendLine("}");

        Compilation.Compile(sb.ToString(), Assembly.GetExecutingAssembly());
        var type = Assembly.GetExecutingAssembly().GetType(Name);
        var typeDef = PostCompilation.GetDefinitionFrom(Assembly.GetExecutingAssembly())?.MainModule.GetType(Name) ?? null;
        if (typeDef == null || type == null)
        {
            throw new InvalidOperationException($"Type {Name} could not be found in the current assembly or the current assembly is not disk-based.");
        }
        foreach (var (fieldDef, field) in typeDef.Fields.OrderBy(x => x.Name).Zip(Fields.OrderBy(x => x.Name)))
        {
            if (fieldDef is not null && field.Value != null)
            {
                fieldDef.Constant = field.Value;
            }
        }
        foreach (var (propertyDef, property) in typeDef.Properties.OrderBy(x => x.Name).Zip(Properties.OrderBy(x => x.Name)))
        {
            if (propertyDef is not null && property.Value != null)
            {
                propertyDef.Constant = property.Value;
            }
        }

        foreach (var (actual, expected) in type.GetMethods().OrderBy(x => x.Name).Zip(Methods.OrderBy(x => x.Name)))
        {
            if (expected.CompiledDelegate != null)
            {
                PostCompilation.MethodInject(actual, prefix: expected.Method);
            }
        }

        return Assembly.GetExecutingAssembly().GetType(Name) ?? throw new InvalidOperationException($"Type {Name} could not be found in the current assembly.");
    }
}