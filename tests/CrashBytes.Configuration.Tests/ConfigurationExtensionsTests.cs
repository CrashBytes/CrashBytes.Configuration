using System.ComponentModel.DataAnnotations;

namespace CrashBytes.Configuration.Tests;

// ──────────────────────────────────────────────
//  Test models
// ──────────────────────────────────────────────

public class AppSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public bool Enabled { get; set; }
    public double Rate { get; set; }
}

public class ExtendedSettings
{
    public string Name { get; set; } = "";
    public long BigNumber { get; set; }
    public decimal Price { get; set; }
    public bool Active { get; set; }
    public TimeSpan Timeout { get; set; }
    public Uri? Endpoint { get; set; }
}

public class ValidatedSettings
{
    [Required]
    public string Name { get; set; } = "";

    [Range(1, 65535)]
    public int Port { get; set; }
}

// ──────────────────────────────────────────────
//  Tests
// ──────────────────────────────────────────────

public class RequireTests
{
    [Fact]
    public void Require_KeyExists_ReturnsValue()
    {
        var config = new Dictionary<string, string> { ["host"] = "localhost" };
        Assert.Equal("localhost", config.Require("host"));
    }

    [Fact]
    public void Require_KeyMissing_ThrowsKeyNotFoundException()
    {
        var config = new Dictionary<string, string>();
        Assert.Throws<KeyNotFoundException>(() => config.Require("host"));
    }

    [Fact]
    public void Require_EmptyValue_ThrowsKeyNotFoundException()
    {
        var config = new Dictionary<string, string> { ["host"] = "" };
        Assert.Throws<KeyNotFoundException>(() => config.Require("host"));
    }

    [Fact]
    public void Require_WhitespaceValue_ThrowsKeyNotFoundException()
    {
        var config = new Dictionary<string, string> { ["host"] = "   " };
        Assert.Throws<KeyNotFoundException>(() => config.Require("host"));
    }

    [Fact]
    public void Require_NullConfig_ThrowsArgumentNullException()
    {
        IDictionary<string, string> config = null!;
        Assert.Throws<ArgumentNullException>(() => config.Require("key"));
    }

    [Fact]
    public void Require_NullKey_ThrowsArgumentNullException()
    {
        var config = new Dictionary<string, string>();
        Assert.Throws<ArgumentNullException>(() => config.Require(null!));
    }
}

public class GetOrDefaultStringTests
{
    [Fact]
    public void GetOrDefault_KeyExists_ReturnsValue()
    {
        var config = new Dictionary<string, string> { ["host"] = "localhost" };
        Assert.Equal("localhost", config.GetOrDefault("host", "fallback"));
    }

    [Fact]
    public void GetOrDefault_KeyMissing_ReturnsDefault()
    {
        var config = new Dictionary<string, string>();
        Assert.Equal("fallback", config.GetOrDefault("host", "fallback"));
    }

    [Fact]
    public void GetOrDefault_EmptyValue_ReturnsDefault()
    {
        var config = new Dictionary<string, string> { ["host"] = "" };
        Assert.Equal("fallback", config.GetOrDefault("host", "fallback"));
    }

    [Fact]
    public void GetOrDefault_NullConfig_ThrowsArgumentNullException()
    {
        IDictionary<string, string> config = null!;
        Assert.Throws<ArgumentNullException>(() => config.GetOrDefault("key"));
    }
}

public class GetOrDefaultTypedTests
{
    [Fact]
    public void GetOrDefault_Int_ValidValue_ReturnsInt()
    {
        var config = new Dictionary<string, string> { ["port"] = "8080" };
        Assert.Equal(8080, config.GetOrDefault("port", 0));
    }

    [Fact]
    public void GetOrDefault_Int_InvalidValue_ReturnsDefault()
    {
        var config = new Dictionary<string, string> { ["port"] = "abc" };
        Assert.Equal(3000, config.GetOrDefault("port", 3000));
    }

    [Fact]
    public void GetOrDefault_Bool_True_ReturnsTrue()
    {
        var config = new Dictionary<string, string> { ["enabled"] = "true" };
        Assert.True(config.GetOrDefault("enabled", false));
    }

    [Fact]
    public void GetOrDefault_Bool_Missing_ReturnsDefault()
    {
        var config = new Dictionary<string, string>();
        Assert.False(config.GetOrDefault("enabled", false));
    }

    [Fact]
    public void GetOrDefault_Double_ValidValue_ReturnsDouble()
    {
        var config = new Dictionary<string, string> { ["rate"] = "3.14" };
        Assert.Equal(3.14, config.GetOrDefault("rate", 0.0));
    }

    [Fact]
    public void GetOrDefault_Double_InvalidValue_ReturnsDefault()
    {
        var config = new Dictionary<string, string> { ["rate"] = "notanumber" };
        Assert.Equal(1.0, config.GetOrDefault("rate", 1.0));
    }

    [Fact]
    public void GetOrDefault_Double_MissingKey_ReturnsDefault()
    {
        var config = new Dictionary<string, string>();
        Assert.Equal(2.5, config.GetOrDefault("rate", 2.5));
    }

    [Fact]
    public void GetOrDefault_Bool_InvalidValue_ReturnsDefault()
    {
        var config = new Dictionary<string, string> { ["flag"] = "notabool" };
        Assert.True(config.GetOrDefault("flag", true));
    }

    [Fact]
    public void GetOrDefault_Int_MissingKey_ReturnsDefault()
    {
        var config = new Dictionary<string, string>();
        Assert.Equal(42, config.GetOrDefault("port", 42));
    }

    [Fact]
    public void GetOrDefault_Int_NullConfig_ThrowsArgumentNullException()
    {
        IDictionary<string, string> config = null!;
        Assert.Throws<ArgumentNullException>(() => config.GetOrDefault("key", 0));
    }

    [Fact]
    public void GetOrDefault_Int_NullKey_ThrowsArgumentNullException()
    {
        var config = new Dictionary<string, string>();
        Assert.Throws<ArgumentNullException>(() => config.GetOrDefault(null!, 0));
    }

    [Fact]
    public void GetOrDefault_Bool_NullConfig_ThrowsArgumentNullException()
    {
        IDictionary<string, string> config = null!;
        Assert.Throws<ArgumentNullException>(() => config.GetOrDefault("key", false));
    }

    [Fact]
    public void GetOrDefault_Bool_NullKey_ThrowsArgumentNullException()
    {
        var config = new Dictionary<string, string>();
        Assert.Throws<ArgumentNullException>(() => config.GetOrDefault(null!, false));
    }

    [Fact]
    public void GetOrDefault_Double_NullConfig_ThrowsArgumentNullException()
    {
        IDictionary<string, string> config = null!;
        Assert.Throws<ArgumentNullException>(() => config.GetOrDefault("key", 0.0));
    }

    [Fact]
    public void GetOrDefault_Double_NullKey_ThrowsArgumentNullException()
    {
        var config = new Dictionary<string, string>();
        Assert.Throws<ArgumentNullException>(() => config.GetOrDefault(null!, 0.0));
    }

    [Fact]
    public void GetOrDefault_String_NullKey_ThrowsArgumentNullException()
    {
        var config = new Dictionary<string, string>();
        Assert.Throws<ArgumentNullException>(() => config.GetOrDefault(null!, "default"));
    }
}

public class BindTests
{
    [Fact]
    public void Bind_MatchingKeys_BindsProperties()
    {
        var config = new Dictionary<string, string>
        {
            ["Host"] = "localhost",
            ["Port"] = "8080",
            ["Enabled"] = "true",
            ["Rate"] = "1.5"
        };

        var settings = config.Bind<AppSettings>();
        Assert.Equal("localhost", settings.Host);
        Assert.Equal(8080, settings.Port);
        Assert.True(settings.Enabled);
        Assert.Equal(1.5, settings.Rate);
    }

    [Fact]
    public void Bind_CaseInsensitive_BindsProperties()
    {
        var config = new Dictionary<string, string>
        {
            ["host"] = "localhost",
            ["port"] = "8080"
        };

        var settings = config.Bind<AppSettings>();
        Assert.Equal("localhost", settings.Host);
        Assert.Equal(8080, settings.Port);
    }

    [Fact]
    public void Bind_WithPrefix_UsesPrefix()
    {
        var config = new Dictionary<string, string>
        {
            ["App:Host"] = "myhost",
            ["App:Port"] = "9090"
        };

        var settings = config.Bind<AppSettings>("App");
        Assert.Equal("myhost", settings.Host);
        Assert.Equal(9090, settings.Port);
    }

    [Fact]
    public void Bind_MissingKeys_LeavesDefaults()
    {
        var config = new Dictionary<string, string> { ["Host"] = "myhost" };
        var settings = config.Bind<AppSettings>();
        Assert.Equal("myhost", settings.Host);
        Assert.Equal(0, settings.Port);
    }

    [Fact]
    public void Bind_NullConfig_ThrowsArgumentNullException()
    {
        IDictionary<string, string> config = null!;
        Assert.Throws<ArgumentNullException>(() => config.Bind<AppSettings>());
    }

    [Fact]
    public void Bind_LongProperty_BindsCorrectly()
    {
        var config = new Dictionary<string, string> { ["BigNumber"] = "9999999999" };
        var settings = config.Bind<ExtendedSettings>();
        Assert.Equal(9999999999L, settings.BigNumber);
    }

    [Fact]
    public void Bind_DecimalProperty_BindsCorrectly()
    {
        var config = new Dictionary<string, string> { ["Price"] = "19.99" };
        var settings = config.Bind<ExtendedSettings>();
        Assert.Equal(19.99m, settings.Price);
    }

    [Fact]
    public void Bind_TimeSpanProperty_BindsCorrectly()
    {
        var config = new Dictionary<string, string> { ["Timeout"] = "00:05:00" };
        var settings = config.Bind<ExtendedSettings>();
        Assert.Equal(TimeSpan.FromMinutes(5), settings.Timeout);
    }

    [Fact]
    public void Bind_UriProperty_BindsCorrectly()
    {
        var config = new Dictionary<string, string> { ["Endpoint"] = "https://api.example.com" };
        var settings = config.Bind<ExtendedSettings>();
        Assert.Equal(new Uri("https://api.example.com"), settings.Endpoint);
    }

    [Fact]
    public void Bind_UriProperty_InvalidUri_LeavesNull()
    {
        var config = new Dictionary<string, string> { ["Endpoint"] = "not a uri" };
        var settings = config.Bind<ExtendedSettings>();
        Assert.Null(settings.Endpoint);
    }

    [Fact]
    public void Bind_UnconvertibleType_LeavesDefault()
    {
        // Port is an int, "abc" cannot be parsed - should remain 0
        var config = new Dictionary<string, string> { ["Port"] = "abc" };
        var settings = config.Bind<AppSettings>();
        Assert.Equal(0, settings.Port);
    }

    [Fact]
    public void Bind_NullValue_LeavesDefault()
    {
        var config = new Dictionary<string, string>();
        // Use a custom dict that returns null value
        config["Host"] = null!;
        var settings = config.Bind<AppSettings>();
        Assert.Equal("", settings.Host);
    }
}

public class ValidateTests
{
    [Fact]
    public void Validate_ValidConfig_ReturnsNoErrors()
    {
        var config = new Dictionary<string, string>
        {
            ["Name"] = "MyApp",
            ["Port"] = "8080"
        };

        var errors = config.Validate<ValidatedSettings>();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingRequired_ReturnsErrors()
    {
        var config = new Dictionary<string, string>
        {
            ["Port"] = "8080"
        };

        var errors = config.Validate<ValidatedSettings>();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_OutOfRange_ReturnsErrors()
    {
        var config = new Dictionary<string, string>
        {
            ["Name"] = "MyApp",
            ["Port"] = "0"
        };

        var errors = config.Validate<ValidatedSettings>();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_NullConfig_ThrowsArgumentNullException()
    {
        IDictionary<string, string> config = null!;
        Assert.Throws<ArgumentNullException>(() => config.Validate<ValidatedSettings>());
    }
}

public class EnvironmentVariableTests
{
    [Fact]
    public void RequireEnvironmentVariable_Exists_ReturnsValue()
    {
        Environment.SetEnvironmentVariable("TEST_CRASHBYTES_VAR", "hello");
        try
        {
            Assert.Equal("hello", ConfigurationExtensions.RequireEnvironmentVariable("TEST_CRASHBYTES_VAR"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_CRASHBYTES_VAR", null);
        }
    }

    [Fact]
    public void RequireEnvironmentVariable_NotSet_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            ConfigurationExtensions.RequireEnvironmentVariable("NONEXISTENT_CRASHBYTES_VAR_12345"));
    }

    [Fact]
    public void RequireEnvironmentVariable_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConfigurationExtensions.RequireEnvironmentVariable(null!));
    }

    [Fact]
    public void GetEnvironmentVariableOrDefault_Exists_ReturnsValue()
    {
        Environment.SetEnvironmentVariable("TEST_CRASHBYTES_VAR2", "world");
        try
        {
            Assert.Equal("world", ConfigurationExtensions.GetEnvironmentVariableOrDefault("TEST_CRASHBYTES_VAR2", "default"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_CRASHBYTES_VAR2", null);
        }
    }

    [Fact]
    public void GetEnvironmentVariableOrDefault_NotSet_ReturnsDefault()
    {
        Assert.Equal("fallback",
            ConfigurationExtensions.GetEnvironmentVariableOrDefault("NONEXISTENT_CRASHBYTES_VAR_12345", "fallback"));
    }
}

public class ParseConnectionStringTests
{
    [Fact]
    public void ParseConnectionString_StandardFormat_ParsesCorrectly()
    {
        var result = ConfigurationExtensions.ParseConnectionString(
            "Server=localhost;Port=5432;Database=mydb;User=admin;Password=secret");

        Assert.Equal("localhost", result["Server"]);
        Assert.Equal("5432", result["Port"]);
        Assert.Equal("mydb", result["Database"]);
        Assert.Equal("admin", result["User"]);
        Assert.Equal("secret", result["Password"]);
    }

    [Fact]
    public void ParseConnectionString_CaseInsensitiveLookup()
    {
        var result = ConfigurationExtensions.ParseConnectionString("Server=myhost");
        Assert.Equal("myhost", result["server"]);
    }

    [Fact]
    public void ParseConnectionString_EmptyString_ReturnsEmpty()
    {
        var result = ConfigurationExtensions.ParseConnectionString("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseConnectionString_TrailingSemicolon_Handled()
    {
        var result = ConfigurationExtensions.ParseConnectionString("Key=Value;");
        Assert.Single(result);
        Assert.Equal("Value", result["Key"]);
    }

    [Fact]
    public void ParseConnectionString_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConfigurationExtensions.ParseConnectionString(null!));
    }

    [Fact]
    public void ParseConnectionString_ValueWithEquals_PreservesValue()
    {
        var result = ConfigurationExtensions.ParseConnectionString("Key=Value=With=Equals");
        Assert.Equal("Value=With=Equals", result["Key"]);
    }

    [Fact]
    public void ParseConnectionString_SegmentWithNoEquals_Skipped()
    {
        var result = ConfigurationExtensions.ParseConnectionString("NoEquals;Key=Value");
        Assert.Single(result);
        Assert.Equal("Value", result["Key"]);
    }

    [Fact]
    public void ParseConnectionString_SegmentStartingWithEquals_Skipped()
    {
        var result = ConfigurationExtensions.ParseConnectionString("=BadValue;Good=Value");
        Assert.Single(result);
        Assert.Equal("Value", result["Good"]);
    }
}
