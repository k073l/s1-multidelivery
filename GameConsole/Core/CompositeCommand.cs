namespace MultiDelivery.GameConsole.Core;

public abstract class CompositeCommand : ICommandNode
{
    private readonly Dictionary<string, ICommandNode> _children = new();

    public abstract string Name { get; }
    public abstract string Description { get; }

    protected void Register(ICommandNode command)
    {
        _children[command.Name] = command;
    }

    public virtual void Execute(CommandContext context)
    {
        if (context.Args.Count == 0)
        {
            PrintHelp(context);
            return;
        }

        var subcommandName = context.Args[0];

        if (!_children.TryGetValue(subcommandName, out var command))
        {
            context.Error($"Unknown subcommand: {subcommandName}");
            return;
        }

        var remaining = context.Args.Skip(1).ToList();

        command.Execute(new CommandContext
        {
            Args = remaining,
            Name = command.Name
        });
    }

    protected void PrintHelp(CommandContext context)
    {
        context.Reply($"{Name} commands:");

        foreach (var child in _children.Values)
        {
            context.Reply($"- {child.Name}: {child.Description}");
        }
    }

    public IEnumerable<ICommandNode> GetChildren()
        => _children.Values;
}