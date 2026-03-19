using MelonLoader;

namespace DeliveryProject;

public class Logger(string categoryName, LogLevel? forceLevel = null)
{
    public void NetworkTrace(params object[] args) => Log(LogLevel.NetworkTrace, args);
    public void Debug(params object[] args) => Log(LogLevel.Debug, args);
    public void Info(params object[] args) => Log(LogLevel.Info, args);
    public void Msg(params object[] args) => Log(LogLevel.Info, args);
    public void Warn(params object[] args) => Log(LogLevel.Warn, args);
    public void Warning(params object[] args) => Log(LogLevel.Warn, args);
    public void Error(params object[] args) => Log(LogLevel.Error, args);

    private void Log(LogLevel level, params object[] args)
    {
        if (forceLevel is { } forced && level < LogLevel.Error)
        {
            level = forced;
        }
        if (args.Length == 0) return;

        string message;
        if (args.Length == 1)
        {
            message = args[0]?.ToString() ?? "";
        }
        else
        {
            var format = args[0]?.ToString() ?? "";
            message = string.Format(format, args.Skip(1).ToArray());
        }

        var ns = string.IsNullOrWhiteSpace(categoryName)
            ? nameof(DeliveryProject)
            : $"{nameof(DeliveryProject)}.{categoryName}";
        var prefix = $"[{ns}] {message}";

        switch (level)
        {
            case LogLevel.NetworkTrace:
                if (DeliveryProject.NetworkLogging.Value) Melon<DeliveryProject>.Logger.Msg(prefix);
                break;
            case LogLevel.Debug:
                MelonDebug.Msg(prefix);
                break;
            case LogLevel.Info:
                Melon<DeliveryProject>.Logger.Msg(prefix);
                break;
            case LogLevel.Warn:
                Melon<DeliveryProject>.Logger.Warning(prefix);
                break;
            case LogLevel.Error:
                Melon<DeliveryProject>.Logger.Error(prefix);
                break;
        }
    }
}

public enum LogLevel
{
    NetworkTrace,
    Debug,
    Info,
    Warn,
    Error
}