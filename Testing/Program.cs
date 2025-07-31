using Onyx.Defense;
using Onyx.Attack;
using Onyx.Shared;

namespace Testing;

public class MyClass(int x)
{
    private int _x = x;

    public void PrintX()
    {
        Console.WriteLine(_x);
    }
}

class Program
{
    static void Main(string[] args)
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
    }
}