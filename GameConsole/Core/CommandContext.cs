using MelonLoader;

namespace MultiDelivery.GameConsole.Core;

public class CommandContext
{
    private readonly Logger _logger;

    public IReadOnlyList<string> Args { get; }

    public string Path { get; }

    public CommandContext(
        string path,
        IReadOnlyList<string> args)
    {
        Path = path;
        Args = args;

        _logger = new Logger($"Console.{Path}");
    }

    public CommandContext CreateChild(
        string childName,
        IReadOnlyList<string> args)
    {
        return new CommandContext(
            $"{Path}.{childName}",
            args);
    }

    public void Reply(string message)
    {
        _logger.Msg(message);
    }

    public void Error(string message)
    {
        _logger.Error(message);
    }
}