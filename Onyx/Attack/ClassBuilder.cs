using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using static Onyx.Shared.GeneralUtilities;

namespace Onyx.Attack;

public static partial class ClassBuilder
{
    public static TypeBuilder CreateTypeBuilder(string typeName,
        TypeAttributes typeAttributes = TypeAttributes.Public | TypeAttributes.Class, Type[]? interfaces = null)
    { // load assembly into memory using the temp context NOT the dynamic assembly stuff
        var asmName = new AssemblyName("dyanamic_" + NewGUID(8, true));
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = asmBuilder.DefineDynamicModule("module0");
        var typeBuilder = moduleBuilder.DefineType(typeName, typeAttributes);
        foreach (var interfaceType in interfaces ?? []) typeBuilder.AddInterfaceImplementation(interfaceType);
        return typeBuilder;
    }

    public static (Type, IL.TempContext) Finalize(TypeBuilder typeBuilder)
    {
        var tctx = new IL.TempContext();
        var type = typeBuilder.CreateType();
        tctx.Track(type.Assembly);
        return (type, tctx);
    }

    public static void FinalizeAndUse(TypeBuilder typeBuilder, Action<Type> action)
    {
        var (type, tctx) = Finalize(typeBuilder);
        action(type);
        tctx.FullUnload();
    }
    
    public static MethodBuilder MethodBuilder(TypeBuilder typeBuilder, string methodName, Type returnType,
        Type[] parameterTypes, MethodAttributes methodAttributes = MethodAttributes.Public)
    {
        return typeBuilder.DefineMethod(methodName, methodAttributes, returnType, parameterTypes);
    }
    
    public static FieldBuilder FieldBuilder(TypeBuilder typeBuilder, string fieldName, Type fieldType,
        FieldAttributes fieldAttributes = FieldAttributes.Public)
    {
        return typeBuilder.DefineField(fieldName, fieldType, fieldAttributes);
    }
    
    public static PropertyBuilder PropertyBuilder(TypeBuilder typeBuilder, string propertyName, Type propertyType,
        PropertyAttributes propertyAttributes = PropertyAttributes.None)
    {
        return typeBuilder.DefineProperty(propertyName, propertyAttributes, propertyType, null);
    }

    public static void DefineOverrideMethod(TypeBuilder typeBuilder, MethodInfo method, MethodInfo originalMethod)
    {
        typeBuilder.DefineMethodOverride(method, originalMethod);
    }

    public static object? New(Type t, object?[] args, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) => 
        Activator.CreateInstance(t, flags, null, args, null);
}