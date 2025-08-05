using Onyx.Attack;
using Onyx.Shared;

namespace Testing;

public static class MyClassTest
{
    public static void Run()
    {
        {
            MyClass myClass = new MyClass(12);
            myClass.PrintX();
                    
            // Huh, I got this object that's being passed around; I wonder if I can exploit it
            var ins = Reflection.Inspect(myClass);
            var privateFields = ins.Fields.Where(x => x.Access.HasFlag(AccessModifier.Private));
            foreach (var field in privateFields)
            { 
                Console.WriteLine($"Name: {field.Name}, Type: {field.Type}, Value: {field.Value}, Access: {field.Access}"); // Lets mess it up
                Reflection.SetField(myClass, field.Name, null!);
            }
                    
            // Now lets try to use MyClass
            myClass.PrintX();
            // _x is now 0 because we set it to null, which is not a valid int
        }
        {
            // Lets trick our user into using a fake constant in their MyClass instance
            // We can do this by creating a new type with an implict int operator
            var dtb = new DynamicTypeBuilder("EvilInt");
            dtb.AddRawMember($"public static implicit operator int(EvilInt evil) {{ Console.WriteLine(\"Evil int is very evil\"); return 42; }}");
            var (evilIntType, tctx) = dtb.Build();
            
            dynamic evilInt = Activator.CreateInstance(evilIntType)!;
            
            // Oh wow I love to use types I've never seen from other libraries
            // Lets use this new "evilInt" type in our MyClass instance
            MyClass myClass = new MyClass(evilInt);
            myClass.PrintX();
            tctx.FullUnload();
        }
    }
}

public class MyClass(int x)
{
    private readonly int _x = x;

    public void PrintX()
    {
        Console.WriteLine(_x.ToString());
    }
}