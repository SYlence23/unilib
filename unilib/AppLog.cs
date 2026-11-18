using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace unilib;

// Application-wide logging. Until Configure() runs, and in tests, loggers discard everything.
// Files go to %LOCALAPPDATA%\UniLib\logs\unilib-YYYYMMDD.log: one per day, at most 10 MB each,
// the last 14 kept. The "Logging" section of the settings can change MinimumLevel and Directory.
// Never log passwords, password hashes, connection strings or email addresses:
// identify users by their id.
public static class AppLog
{
    private const string FileTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static ILoggerFactory Factory { get; private set; } = NullLoggerFactory.Instance;

    // Folder with the log files, or null before Configure().
    public static string? Directory { get; private set; }

    public static ILogger CreateLogger(Type type) => Factory.CreateLogger(type);

    public static ILogger<T> CreateLogger<T>() => Factory.CreateLogger<T>();

    public static void Configure(IConfiguration configuration)
    {
        Factory = CreateFactory(configuration, out var directory);
        Directory = directory;
    }

    // Writes out buffered entries and closes the files.
    public static void Shutdown()
    {
        var factory = Factory;
        Factory = NullLoggerFactory.Instance;
        factory.Dispose();
    }

    public static ILoggerFactory CreateFactory(IConfiguration configuration, out string directory)
    {
        var section = configuration.GetSection("Logging");
        directory = section["Directory"] is { Length: > 0 } configured
            ? Environment.ExpandEnvironmentVariables(configured)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniLib", "logs");
        var level = Enum.TryParse<LogEventLevel>(section["MinimumLevel"], ignoreCase: true, out var parsed)
            ? parsed
            : LogEventLevel.Information;

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            // EF Core and other framework logs: only warnings and errors.
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.File(Path.Combine(directory, "unilib-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                // Two running copies of the app can share the same file.
                shared: true,
                outputTemplate: FileTemplate)
            .CreateLogger();
        return new SerilogLoggerFactory(logger, dispose: true);
    }
}
