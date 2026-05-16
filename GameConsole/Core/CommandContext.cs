using MelonLoader;

namespace MultiDelivery.GameConsole.Core;

public class CommandContext
{
    private MelonLogger.Instance _logger;
    public List<string> Args { get; set; } = [];
    public string Name { get; set; }

    public CommandContext()
    {
        _logger = new MelonLogger.Instance($"{nameof(MultiDelivery)}.Console.{Name}");
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