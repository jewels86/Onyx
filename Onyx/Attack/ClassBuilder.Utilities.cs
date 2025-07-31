using System.Reflection;

namespace Onyx.Attack;

public enum OperatorType
{
    Addition, Subtraction, Multiplication, Division, Modulus,
    BitwiseAnd, BitwiseOr, BitwiseXor, BitwiseNot, Left,
    Shift, RightShift, LogicalAnd, LogicalOr, LogicalXor,
    Equal, NotEqual, GreaterThan, LessThan, GreaterThanOrEqual,
    Implicit, Explicit
}

public static partial class ClassBuilder
{
    public static string OperatorToString(OperatorType op)
    {
        switch (op)
        {
            case OperatorType.Addition:
                return "op_Addition";
            case OperatorType.Subtraction:
                return "op_Subtraction";
            case OperatorType.Multiplication:
                return "op_Multiplication";
            case OperatorType.Division:
                return "op_Division";
            case OperatorType.Implicit:
                return "op_Implicit";
            case OperatorType.Explicit:
                return "op_Explicit";
            default:
                return "op_" + op.ToString();
        }
    }
    
    public static MethodAttributes OperatorMethodAttributes()
    {
        return MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
    }
}