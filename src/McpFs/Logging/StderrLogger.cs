namespace McpFs.Logging;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warn = 2,
    Error = 3
}

public sealed class StderrLogger
{
    private readonly LogLevel _minimumLevel;
    private readonly object _sync = new();

    public StderrLogger(string? minimumLevel)
    {
        _minimumLevel = Parse(minimumLevel);
    }

    public void Debug(string message) => Write(LogLevel.Debug, message);
    public void Info(string message) => Write(LogLevel.Info, message);
    public void Warn(string message) => Write(LogLevel.Warn, message);
    public void Error(string message) => Write(LogLevel.Error, message);

    private void Write(LogLevel level, string message)
    {
        if (level < _minimumLevel)
        {
            return;
        }

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        lock (_sync)
        {
            Console.Error.WriteLine($"[{timestamp}] [{level}] {message}");
        }
    }

    private static LogLevel Parse(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return LogLevel.Info;
        }

        return level.Trim().ToLowerInvariant() switch
        {
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "warn" or "warning" => LogLevel.Warn,
            "error" => LogLevel.Error,
            _ => LogLevel.Info
        };
    }
}
