using Spectre.Console.Cli;
using Onyx.Attack;
using Spectre.Console;

namespace Onyx.Command;

public class PocCommand : Command<PocCommand.PocCommandSettings>
{
    public class PocCommandSettings : CommandSettings
    {
        [CommandArgument(0, "[udcTargetPath]")]
        public required string UdcTargetPath { get; init; }
        
        [CommandArgument(1, "[dependenciesDirectory]")]
        public required string DependenciesDirectory { get; init; }
        
        [CommandArgument(2, "[outputPath]")]
        public required string OutputPath { get; init; }
    }

    public override int Execute(CommandContext context, PocCommandSettings settings)
    {
        AnsiConsole.WriteLine("[blue]Creating POC...[/]");
        PocCreation.Create(settings.UdcTargetPath, settings.DependenciesDirectory, settings.OutputPath);

        AnsiConsole.MarkupLine("[green]POC created successfully![/]");
        return 0;
    }
}