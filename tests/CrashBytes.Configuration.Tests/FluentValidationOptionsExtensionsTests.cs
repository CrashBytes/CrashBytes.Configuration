using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CrashBytes.Configuration.Tests;

// ──────────────────────────────────────────────
//  Test models + validators
// ──────────────────────────────────────────────

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = "";
    public int CommandTimeoutSeconds { get; set; }
}

public class DatabaseOptionsValidator : AbstractValidator<DatabaseOptions>
{
    public DatabaseOptionsValidator()
    {
        RuleFor(x => x.ConnectionString).NotEmpty();
        RuleFor(x => x.CommandTimeoutSeconds).InclusiveBetween(1, 300);
    }
}

// ──────────────────────────────────────────────
//  AddFluentValidatedOptions — validation at resolution
// ──────────────────────────────────────────────

public class AddFluentValidatedOptionsTests
{
    [Fact]
    public void AddFluentValidatedOptions_ValidConfig_BindsAndResolves()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=db;",
                ["Database:CommandTimeoutSeconds"] = "30"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFluentValidatedOptions<DatabaseOptions, DatabaseOptionsValidator>(
            configuration, "Database");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        Assert.Equal("Server=db;", options.ConnectionString);
        Assert.Equal(30, options.CommandTimeoutSeconds);
    }

    [Fact]
    public void AddFluentValidatedOptions_InvalidConfig_ThrowsOnResolution()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "",
                ["Database:CommandTimeoutSeconds"] = "0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFluentValidatedOptions<DatabaseOptions, DatabaseOptionsValidator>(
            configuration, "Database");

        using var provider = services.BuildServiceProvider();
        var ex = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);

        // FluentValidation messages are surfaced via OptionsValidationException
        Assert.Contains(ex.Failures, m => m.Contains("ConnectionString"));
        Assert.Contains(ex.Failures, m => m.Contains("CommandTimeoutSeconds"));
    }

    [Fact]
    public void AddFluentValidatedOptions_BindsRootWhenSectionNameOmitted()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "Server=root;",
                ["CommandTimeoutSeconds"] = "60"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFluentValidatedOptions<DatabaseOptions, DatabaseOptionsValidator>(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        Assert.Equal("Server=root;", options.ConnectionString);
        Assert.Equal(60, options.CommandTimeoutSeconds);
    }

    [Fact]
    public void AddFluentValidatedOptions_RegistersValidatorInDI()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddFluentValidatedOptions<DatabaseOptions, DatabaseOptionsValidator>(configuration);

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<DatabaseOptions>>();
        Assert.IsType<DatabaseOptionsValidator>(validator);
    }

    [Fact]
    public void AddFluentValidatedOptions_NullServices_Throws()
    {
        IServiceCollection services = null!;
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddFluentValidatedOptions<DatabaseOptions, DatabaseOptionsValidator>(configuration));
    }

    [Fact]
    public void AddFluentValidatedOptions_NullConfiguration_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddFluentValidatedOptions<DatabaseOptions, DatabaseOptionsValidator>(null!));
    }
}

// ──────────────────────────────────────────────
//  AddFluentValidatedOptionsOnStart — fail-fast at startup
// ──────────────────────────────────────────────

public class AddFluentValidatedOptionsOnStartTests
{
    [Fact]
    public async Task AddFluentValidatedOptionsOnStart_InvalidConfig_FailsHostStart()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "",
                ["Database:CommandTimeoutSeconds"] = "999"
            }))
            .ConfigureServices((ctx, services) =>
                services.AddFluentValidatedOptionsOnStart<DatabaseOptions, DatabaseOptionsValidator>(
                    ctx.Configuration, "Database"));

        using var host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task AddFluentValidatedOptionsOnStart_ValidConfig_StartsCleanly()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=ok;",
                ["Database:CommandTimeoutSeconds"] = "15"
            }))
            .ConfigureServices((ctx, services) =>
                services.AddFluentValidatedOptionsOnStart<DatabaseOptions, DatabaseOptionsValidator>(
                    ctx.Configuration, "Database"));

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        var options = host.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        Assert.Equal("Server=ok;", options.ConnectionString);
    }
}

// ──────────────────────────────────────────────
//  Bridge specifics (FluentValidateOptions surface)
// ──────────────────────────────────────────────

public class FluentValidateOptionsBridgeTests
{
    [Fact]
    public void Bridge_CollectsAllErrorsFromValidator()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionString"] = "",
                ["CommandTimeoutSeconds"] = "0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddFluentValidatedOptions<DatabaseOptions, DatabaseOptionsValidator>(configuration);

        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<DatabaseOptions>>().Value);

        // Both rules should fire; validator does NOT short-circuit on first failure
        Assert.True(ex.Failures.Count() >= 2);
    }
}
