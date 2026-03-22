using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CrashBytes.Configuration;

/// <summary>
/// Configuration validation and binding helpers. Works with dictionaries and environment variables
/// without requiring Microsoft.Extensions.Configuration.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets a required configuration value. Throws if the key is missing or the value is empty.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <c>null</c>.</exception>
    /// <exception cref="KeyNotFoundException">Key is not found or value is empty.</exception>
    public static string Require(this IDictionary<string, string> config, string key)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (key is null) throw new ArgumentNullException(nameof(key));

        if (!config.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new KeyNotFoundException($"Required configuration key '{key}' is missing or empty.");

        return value;
    }

    /// <summary>
    /// Gets a configuration value or returns <paramref name="defaultValue"/> if the key is missing.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <c>null</c>.</exception>
    public static string GetOrDefault(this IDictionary<string, string> config, string key, string defaultValue = "")
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (key is null) throw new ArgumentNullException(nameof(key));

        return config.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as <see cref="int"/> or returns <paramref name="defaultValue"/>.
    /// </summary>
    public static int GetOrDefault(this IDictionary<string, string> config, string key, int defaultValue)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (key is null) throw new ArgumentNullException(nameof(key));

        return config.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as <see cref="bool"/> or returns <paramref name="defaultValue"/>.
    /// </summary>
    public static bool GetOrDefault(this IDictionary<string, string> config, string key, bool defaultValue)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (key is null) throw new ArgumentNullException(nameof(key));

        return config.TryGetValue(key, out var value) && bool.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as <see cref="double"/> or returns <paramref name="defaultValue"/>.
    /// </summary>
    public static double GetOrDefault(this IDictionary<string, string> config, string key, double defaultValue)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        if (key is null) throw new ArgumentNullException(nameof(key));

        return config.TryGetValue(key, out var value) && double.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Binds configuration keys to properties of <typeparamref name="T"/> by matching property names (case-insensitive).
    /// Supports string, int, long, double, decimal, and bool properties.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <c>null</c>.</exception>
    public static T Bind<T>(this IDictionary<string, string> config, string? prefix = null) where T : new()
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        var instance = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var prop in properties)
        {
            var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}:{prop.Name}";

            // Try exact match first, then case-insensitive
            if (!config.TryGetValue(key, out var value))
            {
                var match = config.Keys.FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (match is null) continue;
                value = config[match];
            }

            if (value is null) continue;

            var converted = ConvertValue(value, prop.PropertyType);
            if (converted is not null)
                prop.SetValue(instance, converted);
        }

        return instance;
    }

    /// <summary>
    /// Validates the configuration values against <see cref="DataAnnotations"/> attributes on the properties of <typeparamref name="T"/>.
    /// Returns a list of validation errors (empty if valid).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <c>null</c>.</exception>
    public static IReadOnlyList<string> Validate<T>(this IDictionary<string, string> config, string? prefix = null) where T : new()
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        var instance = config.Bind<T>(prefix);
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);

        return results.Select(r => r.ErrorMessage ?? "Validation failed.").ToList();
    }

    /// <summary>
    /// Gets a required environment variable. Throws if it is not set or empty.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Environment variable is not set or empty.</exception>
    public static string RequireEnvironmentVariable(string name)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new KeyNotFoundException($"Required environment variable '{name}' is not set or empty.");

        return value;
    }

    /// <summary>
    /// Gets an environment variable or returns <paramref name="defaultValue"/>.
    /// </summary>
    public static string GetEnvironmentVariableOrDefault(string name, string defaultValue = "")
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    /// <summary>
    /// Parses a connection string into a dictionary of key-value pairs.
    /// </summary>
    public static IDictionary<string, string> ParseConnectionString(string connectionString)
    {
        if (connectionString is null) throw new ArgumentNullException(nameof(connectionString));

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIndex = segment.IndexOf('=');
            if (eqIndex <= 0) continue;

            var key = segment.Substring(0, eqIndex).Trim();
            var value = segment.Substring(eqIndex + 1).Trim();

            if (key.Length > 0)
                result[key] = value;
        }

        return result;
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(int) && int.TryParse(value, out var i)) return i;
        if (targetType == typeof(long) && long.TryParse(value, out var l)) return l;
        if (targetType == typeof(double) && double.TryParse(value, out var d)) return d;
        if (targetType == typeof(decimal) && decimal.TryParse(value, out var m)) return m;
        if (targetType == typeof(bool) && bool.TryParse(value, out var b)) return b;
        if (targetType == typeof(TimeSpan) && TimeSpan.TryParse(value, out var ts)) return ts;
        if (targetType == typeof(Uri)) return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
        return null;
    }
}
