using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CrashBytes.Configuration.Tests;

// ──────────────────────────────────────────────
//  Test models for the DI-integrated validators
// ──────────────────────────────────────────────

public class HostOptions
{
    [Required]
    public string Host { get; set; } = "";

    [Range(1, 65535)]
    public int Port { get; set; }
}

public class CacheOptions
{
    [Required, MinLength(1)]
    public string Region { get; set; } = "";
}

// ──────────────────────────────────────────────
//  AddValidatedOptions (DataAnnotations) — happy path
// ──────────────────────────────────────────────

public class AddValidatedOptionsTests
{
    [Fact]
    public void AddValidatedOptions_ValidConfig_BindsAndResolves()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Service:Host"] = "localhost",
                ["Service:Port"] = "8080"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddValidatedOptions<HostOptions>(configuration, "Service");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HostOptions>>().Value;

        Assert.Equal("localhost", options.Host);
        Assert.Equal(8080, options.Port);
    }

    [Fact]
    public void AddValidatedOptions_BindsRootWhenSectionNameOmitted()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Host"] = "myhost",
                ["Port"] = "9999"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddValidatedOptions<HostOptions>(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HostOptions>>().Value;

        Assert.Equal("myhost", options.Host);
        Assert.Equal(9999, options.Port);
    }

    [Fact]
    public void AddValidatedOptions_InvalidConfig_ThrowsOnResolution()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Service:Host"] = "",        // [Required] violation
                ["Service:Port"] = "70000"    // [Range] violation
            })
            .Build();

        var services = new ServiceCollection();
        services.AddValidatedOptions<HostOptions>(configuration, "Service");

        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<HostOptions>>().Value);
    }

    [Fact]
    public void AddValidatedOptions_NullServices_Throws()
    {
        IServiceCollection services = null!;
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddValidatedOptions<HostOptions>(configuration));
    }

    [Fact]
    public void AddValidatedOptions_NullConfiguration_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddValidatedOptions<HostOptions>(null!));
    }

    [Fact]
    public void AddValidatedOptions_ReturnsBuilderForChaining()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Host"] = "h",
                ["Port"] = "1"
            })
            .Build();

        var services = new ServiceCollection();
        var builder = services.AddValidatedOptions<HostOptions>(configuration);

        Assert.NotNull(builder);
        Assert.Equal(typeof(HostOptions), builder.GetType().GenericTypeArguments[0]);
    }
}

// ──────────────────────────────────────────────
//  AddValidatedOptionsOnStart — fail-fast at host startup
// ──────────────────────────────────────────────

public class AddValidatedOptionsOnStartTests
{
    [Fact]
    public async Task AddValidatedOptionsOnStart_InvalidConfig_FailsHostStart()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Service:Host"] = "",
                ["Service:Port"] = "0"
            }))
            .ConfigureServices((ctx, services) =>
                services.AddValidatedOptionsOnStart<HostOptions>(ctx.Configuration, "Service"));

        using var host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task AddValidatedOptionsOnStart_ValidConfig_StartsCleanly()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Service:Host"] = "valid",
                ["Service:Port"] = "443"
            }))
            .ConfigureServices((ctx, services) =>
                services.AddValidatedOptionsOnStart<HostOptions>(ctx.Configuration, "Service"));

        using var host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();

        var options = host.Services.GetRequiredService<IOptions<HostOptions>>().Value;
        Assert.Equal("valid", options.Host);
        Assert.Equal(443, options.Port);
    }
}

// ──────────────────────────────────────────────
//  ValidateOptionsOnStart — opt-in for an existing registration
// ──────────────────────────────────────────────

public class ValidateOptionsOnStartTests
{
    [Fact]
    public async Task ValidateOptionsOnStart_AfterPriorRegistration_FailsHostStart()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Region"] = ""
            }))
            .ConfigureServices((ctx, services) =>
            {
                services.AddOptions<CacheOptions>()
                    .Bind(ctx.Configuration)
                    .ValidateDataAnnotations();

                services.ValidateOptionsOnStart<CacheOptions>();
            });

        using var host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public void ValidateOptionsOnStart_NullServices_Throws()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() =>
            services.ValidateOptionsOnStart<CacheOptions>());
    }

    [Fact]
    public void ValidateOptionsOnStart_ReturnsBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.ValidateOptionsOnStart<CacheOptions>();
        Assert.NotNull(builder);
    }
}
