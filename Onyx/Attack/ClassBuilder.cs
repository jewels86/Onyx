using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;

namespace Onyx.Attack;

public static partial class ClassBuilder
{
    public static TypeBuilder CreateTypeBuilder(string assemblyName, string moduleName, string typeName, 
        TypeAttributes typeAttributes = TypeAttributes.Public | TypeAttributes.Class, Type[]? interfaces = null)
    {
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
        var module = asm.DefineDynamicModule(moduleName);
        return module.DefineType(typeName, typeAttributes, null, interfaces ?? []);
    }

    public static Type Finalize(TypeBuilder typeBuilder)
    {
        return typeBuilder.CreateType();
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

    public static object? New(Type t) => Activator.CreateInstance(t);
}