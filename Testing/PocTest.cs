using Microsoft.CodeAnalysis;
using Onyx.Attack;
using Compilation = Onyx.Shared.Compilation;

namespace Testing;

public static class PocTest
{
    public static void Run()
    {
        string code = @"
            using Onyx.Shared;
            using System;
            
            public class EvilTestPlugin 
            {
                public void DoSomething() 
                {
                    Console.WriteLine(""mwha ha ha ha ha"");
                    var result = Compilation.Run(""1 + 1"").Result;
                    Console.WriteLine(""I am the meanest around, 1 + 1 = {0}"", result);
                }
            }
        ";

        
    }
}