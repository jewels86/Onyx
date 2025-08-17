using Onyx.Shared;
using Spectre.Console.Cli;

namespace Onyx.Command;

public class Program
{
    public static int Main(string[] args)
    {
        CommandApp app = new();
        app.Configure(config =>
        {
            config.AddCommand<PocCommand>("poc")
                .WithDescription("Create a packed onyx compilation (POC) for a UDC target with dependencies.");
        });
        return app.Run(args);
    }
}