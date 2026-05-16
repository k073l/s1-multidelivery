using MelonLoader;

namespace MultiDelivery.GameConsole.Core;

public class CommandContext
{
    private readonly MelonLogger.Instance _logger;

    public IReadOnlyList<string> Args { get; }

    public string Path { get; }

    public CommandContext(
        string path,
        IReadOnlyList<string> args)
    {
        Path = path;
        Args = args;

        _logger = new MelonLogger.Instance(
            $"{nameof(MultiDelivery)}.Console.{Path}");
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