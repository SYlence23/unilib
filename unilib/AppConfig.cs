using Microsoft.Extensions.Configuration;

namespace unilib;

// Application settings. Later sources override earlier ones:
// 1. appsettings.json: committed, holds no secrets;
// 2. appsettings.Local.json: git-ignored, per machine (the database password goes here);
// 3. environment variables with the UNILIB_ prefix, e.g. UNILIB_ConnectionStrings__Default.
public static class AppConfig
{
    public const string EnvironmentPrefix = "UNILIB_";

    private static readonly Lazy<IConfiguration> Current = new(() => Build(AppContext.BaseDirectory));

    public static IConfiguration Configuration => Current.Value;

    public static IConfiguration Build(string basePath) => new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings.Local.json", optional: true)
        .AddEnvironmentVariables(EnvironmentPrefix)
        .Build();
}
