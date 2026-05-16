using MultiDelivery.GameConsole.Commands;
using MultiDelivery.GameConsole.Core;
using S1API.Console;

namespace MultiDelivery.GameConsole;

public class RootCommand: BaseConsoleCommand
{
    private readonly RootNode _rootNode;

    public RootCommand()
    {
        _rootNode = new RootNode();
    }
    
    public override void ExecuteCommand(List<string> args)
    {
        _rootNode.Execute(new CommandContext
        {
            Args = args,
            Name = _rootNode.Name
        });
    }

    public override string CommandWord => "multidelivery";
    public override string CommandDescription => "Root command for MultiDelivery mod. Use subcommands for specific actions. Output is presented in MelonLoader console window.";
    public override string ExampleUsage => "multidelivery help";
}