using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrashBytes.Configuration;

/// <summary>
/// DI extensions that bind <see cref="IOptions{TOptions}"/> instances to <see cref="IConfiguration"/>
/// sections and wire <see cref="System.ComponentModel.DataAnnotations"/> validation with eager,
/// fail-fast behaviour at application startup.
/// </summary>
public static class OptionsValidationExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TOptions"/>, binds it to the supplied
    /// <see cref="IConfiguration"/> section, and validates it using
    /// <c>System.ComponentModel.DataAnnotations</c> attributes. Validation runs at the moment
    /// the options instance is first resolved.
    /// </summary>
    /// <typeparam name="TOptions">The options type to register. Must be a reference type with a
    /// public parameterless constructor.</typeparam>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The configuration root or section that contains the options
    /// values.</param>
    /// <param name="sectionName">Optional configuration section name. When <c>null</c> or empty,
    /// <paramref name="configuration"/> is bound directly.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so callers can layer further
    /// configuration (e.g. <c>.Validate(...)</c> or <c>.ValidateOnStart()</c>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or
    /// <paramref name="configuration"/> is <c>null</c>.</exception>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
        where TOptions : class
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var section = string.IsNullOrEmpty(sectionName)
            ? configuration
            : configuration.GetSection(sectionName);

        return services
            .AddOptions<TOptions>()
            .Bind(section)
            .ValidateDataAnnotations();
    }

    /// <summary>
    /// Same as <see cref="AddValidatedOptions{TOptions}(IServiceCollection, IConfiguration, string?)"/>
    /// but additionally invokes <see cref="OptionsBuilderExtensions.ValidateOnStart{TOptions}"/>,
    /// causing validation to run when the host starts. Combine with a hosted application
    /// (<c>Host.CreateApplicationBuilder</c>, ASP.NET Core <c>WebApplication</c>, etc.) so that
    /// invalid configuration prevents startup.
    /// </summary>
    /// <typeparam name="TOptions">The options type to register.</typeparam>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The configuration root or section that contains the options
    /// values.</param>
    /// <param name="sectionName">Optional configuration section name.</param>
    /// <returns>The configured <see cref="OptionsBuilder{TOptions}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or
    /// <paramref name="configuration"/> is <c>null</c>.</exception>
    public static OptionsBuilder<TOptions> AddValidatedOptionsOnStart<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
        where TOptions : class
    {
        return services
            .AddValidatedOptions<TOptions>(configuration, sectionName)
            .ValidateOnStart();
    }

    /// <summary>
    /// Forces an existing options registration to validate at host startup. Equivalent to calling
    /// <see cref="OptionsBuilderExtensions.ValidateOnStart{TOptions}"/> on the builder returned by
    /// <see cref="OptionsServiceCollectionExtensions.AddOptions{TOptions}(IServiceCollection)"/>.
    /// Use this when an options type was registered elsewhere (for example by another package) and
    /// you want to opt that registration into fail-fast startup validation.
    /// </summary>
    /// <typeparam name="TOptions">The options type whose registration should be validated on
    /// start.</typeparam>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static OptionsBuilder<TOptions> ValidateOptionsOnStart<TOptions>(
        this IServiceCollection services)
        where TOptions : class
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        return services.AddOptions<TOptions>().ValidateOnStart();
    }
}
