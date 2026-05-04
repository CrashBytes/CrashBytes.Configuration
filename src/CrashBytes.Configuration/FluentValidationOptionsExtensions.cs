using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrashBytes.Configuration;

/// <summary>
/// DI extensions that bind <see cref="IOptions{TOptions}"/> instances to <see cref="IConfiguration"/>
/// sections and validate them with a <see cref="FluentValidation.IValidator{T}"/> at resolution
/// time and (optionally) at host startup.
/// </summary>
public static class FluentValidationOptionsExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TOptions"/>, binds it to the supplied configuration section,
    /// and registers <typeparamref name="TValidator"/> as the FluentValidation validator for it.
    /// Validation executes whenever the options instance is materialised. Combine with
    /// <see cref="OptionsBuilderExtensions.ValidateOnStart{TOptions}"/> (or use
    /// <see cref="AddFluentValidatedOptionsOnStart{TOptions, TValidator}"/>) to fail fast at
    /// application startup.
    /// </summary>
    /// <typeparam name="TOptions">The options type to register.</typeparam>
    /// <typeparam name="TValidator">The FluentValidation validator type.</typeparam>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The configuration root or section.</param>
    /// <param name="sectionName">Optional configuration section name. When <c>null</c> or empty,
    /// <paramref name="configuration"/> is bound directly.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or
    /// <paramref name="configuration"/> is <c>null</c>.</exception>
    public static OptionsBuilder<TOptions> AddFluentValidatedOptions<TOptions, TValidator>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
        where TOptions : class
        where TValidator : class, IValidator<TOptions>
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var section = string.IsNullOrEmpty(sectionName)
            ? configuration
            : configuration.GetSection(sectionName);

        services.AddSingleton<IValidator<TOptions>, TValidator>();
        services.AddSingleton<IValidateOptions<TOptions>, FluentValidateOptions<TOptions>>();

        return services
            .AddOptions<TOptions>()
            .Bind(section);
    }

    /// <summary>
    /// Same as
    /// <see cref="AddFluentValidatedOptions{TOptions, TValidator}(IServiceCollection, IConfiguration, string?)"/>
    /// but additionally invokes <see cref="OptionsBuilderExtensions.ValidateOnStart{TOptions}"/>,
    /// causing validation to run during host startup. Invalid configuration prevents the host from
    /// starting.
    /// </summary>
    /// <typeparam name="TOptions">The options type to register.</typeparam>
    /// <typeparam name="TValidator">The FluentValidation validator type.</typeparam>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The configuration root or section.</param>
    /// <param name="sectionName">Optional configuration section name.</param>
    /// <returns>The configured <see cref="OptionsBuilder{TOptions}"/>.</returns>
    public static OptionsBuilder<TOptions> AddFluentValidatedOptionsOnStart<TOptions, TValidator>(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
        where TOptions : class
        where TValidator : class, IValidator<TOptions>
    {
        return services
            .AddFluentValidatedOptions<TOptions, TValidator>(configuration, sectionName)
            .ValidateOnStart();
    }
}

/// <summary>
/// Bridges a registered FluentValidation <see cref="IValidator{T}"/> into the
/// <see cref="IValidateOptions{TOptions}"/> contract used by the Options framework.
/// </summary>
/// <typeparam name="TOptions">The options type being validated.</typeparam>
internal sealed class FluentValidateOptions<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initialises a new instance of the <see cref="FluentValidateOptions{TOptions}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The DI container, used to resolve a fresh validator instance
    /// each time validation runs (so the validator may itself depend on scoped services).</param>
    public FluentValidateOptions(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail($"Options instance of type {typeof(TOptions).Name} was null.");
        }

        using var scope = _serviceProvider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IValidator<TOptions>>();

        var result = validator.Validate(options);
        if (result.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = result.Errors
            .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
            .ToArray();

        return ValidateOptionsResult.Fail(failures);
    }
}
