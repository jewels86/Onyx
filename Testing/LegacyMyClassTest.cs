using System.Reflection;
using System.Reflection.Emit;
using Onyx.Attack;
using Onyx.Shared;

public static class LegacyMyClassTest
{
    public static void Run()
    {
        MyClass myClass = new MyClass(12);
        myClass.PrintX();
        

        var field = Reflection.GetField(myClass, "_x");
        Console.WriteLine($"Name: {field.Name}, Type: {field.Type}, Value: {field.Value}, Access: {field.Access}, Result: {field.Result}");
        Reflection.SetField(myClass, "_x", 123);
        myClass.PrintX();
        
        Reflection.InspectionResult inspectionResult = Reflection.Inspect(myClass);
        Console.WriteLine($"Fields: {inspectionResult.Fields.Count}, Properties: {inspectionResult.Properties.Count}");
        foreach (var fieldPackage in inspectionResult.Fields)
        {
            Console.WriteLine($"Field: {fieldPackage.Name}, Value: {fieldPackage.Value}, Access: {fieldPackage.Access}");
        }

        var tb = LegacyClassBuilder.CreateTypeBuilder("EvilInt");

        var method = LegacyClassBuilder.MethodBuilder(
            tb, 
            LegacyClassBuilder.OperatorToString(OperatorType.Implicit), 
            typeof(int), 
            [tb], 
            LegacyClassBuilder.OperatorMethodAttributes());
        var il = method.GetILGenerator();
        il.EmitWriteLine("This is an evil implicit operator");
        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Ret);
        
        var (evilIntType, tctx) = LegacyClassBuilder.Finalize(tb);
        {
            dynamic evilInt = LegacyClassBuilder.New(evilIntType, [])!;
        
            // Oh wow I love to use types I've never seen from other libraries
            // Lets use this new "evilInt" type in our MyClass instance
            int evil = evilInt; // This will call the implicit operator we defined
            Console.WriteLine($"Evil Int: {evil}");
            Reflection.SetField(myClass, "_x", evilInt);
            myClass.PrintX();
        }
        tctx.FullUnload();
        Console.WriteLine(tctx.IsAlive);

        var mytb = LegacyClassBuilder.CreateTypeBuilder("MyClass");
        var fb = LegacyClassBuilder.FieldBuilder(mytb, "maybeSecure", typeof(string));
        fb.SetConstant("hello");
        LegacyClassBuilder.FinalizeAndUse(mytb, t =>
        {
            dynamic x = LegacyClassBuilder.New(mytb, [])!;
            Console.WriteLine(Reflection.GetField(x, "maybeSecure"));
            return null;
        });
    }
    
    public class MyClass(int x)
    {
        private int _x = x;

        public void PrintX()
        {
            Console.WriteLine(_x.ToString());
        }
    }
}