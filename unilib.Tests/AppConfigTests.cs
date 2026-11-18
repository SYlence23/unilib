using Microsoft.Extensions.Configuration;

namespace unilib.Tests;

public class AppConfigTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("unilib-config-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private void Write(string file, string connectionString) =>
        File.WriteAllText(Path.Combine(_dir, file),
            $$"""{ "ConnectionStrings": { "Default": "{{connectionString}}" } }""");

    [Fact]
    public void CommittedAppSettings_HasNoPassword()
    {
        var committed = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
            .Build()
            .GetConnectionString("Default");

        Assert.NotNull(committed);
        Assert.DoesNotContain("password", committed, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocalFile_OverridesCommittedSettings()
    {
        Write("appsettings.json", "Host=shared");
        Write("appsettings.Local.json", "Host=local;Password=secret");

        Assert.Equal("Host=local;Password=secret", AppConfig.Build(_dir).GetConnectionString("Default"));
    }

    [Fact]
    public void WithoutLocalFile_UsesCommittedSettings()
    {
        Write("appsettings.json", "Host=shared");

        Assert.Equal("Host=shared", AppConfig.Build(_dir).GetConnectionString("Default"));
    }

    [Fact]
    public void PrefixedEnvironmentVariable_OverridesFiles()
    {
        Write("appsettings.json", "Host=shared");
        const string name = AppConfig.EnvironmentPrefix + "AppConfigTests__Value";
        Environment.SetEnvironmentVariable(name, "from-env");
        try
        {
            Assert.Equal("from-env", AppConfig.Build(_dir)["AppConfigTests:Value"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }
}
