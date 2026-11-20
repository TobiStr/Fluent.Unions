using System.Globalization;
using Fluent.Unions.Validation;
using Is = NUnit.Framework.Is;
using Rule = Fluent.Unions.Validation.Is;

namespace Fluent.Unions.Scenarios.Tests.Configuration;

public record ServerSettings(string Host, int Port, TimeSpan Timeout, int MaxConnections, Option<string> Proxy);

/// <summary>
/// Loads settings from a file and the environment. Shows no <c>null</c>: a missing setting is an option, a
/// default is chosen explicitly, a present but invalid setting is an error, and all invalid settings are
/// reported together.
/// </summary>
public static class SettingsLoader
{
    private const int DefaultMaxConnections = 10;

    public static Result<ServerSettings> Load(
        IReadOnlyDictionary<string, string> file,
        IReadOnlyDictionary<string, string> environment)
    {
        Option<string> Lookup(string key) => environment
            .GetValueOrNone($"APP_{key.ToUpperInvariant()}")
            .Where(Rule.NotWhiteSpace)
            .Or(() => file.GetValueOrNone(key).Where(Rule.NotWhiteSpace));

        Result<string> Required(string key) =>
            Lookup(key).ToResult(() => new ValidationError($"{key} is required.", key));

        Result<int> Number(string key, int fallback) =>
            Lookup(key)
                .Select(text => Result.Try(() => int.Parse(text, CultureInfo.InvariantCulture))
                    .Catch((FormatException _) => new ValidationError($"{key} must be a whole number.", key)))
                .Transpose()
                .Select(number => number.GetValueOrDefault(fallback));

        var proxy = Lookup("ProxyHost").Zip(
            Lookup("ProxyPort").Chain(port => Option.TryParse<int>(port, CultureInfo.InvariantCulture)),
            (host, port) => $"{host}:{port}");

        var maxConnections = Lookup("MaxConnections")
            .Chain(text => Option.TryParse<int>(text, CultureInfo.InvariantCulture))
            .Where(Rule.Positive)
            .GetValueOrDefault(DefaultMaxConnections);

        return Result.CombineAll(
                Required("Host"),
                Number("Port", 8080).EnsureInRange(1, 65535, name: "Port"),
                Number("TimeoutSeconds", 30).EnsurePositive(name: "TimeoutSeconds"))
            .Select(parsed => new ServerSettings(
                parsed.Item1, parsed.Item2, TimeSpan.FromSeconds(parsed.Item3), maxConnections, proxy));
    }
}

public class ConfigurationScenarioTests
{
    private static readonly Dictionary<string, string> NoEnvironment = [];

    [Test]
    public void DefaultsFillTheMissingSettings()
    {
        var settings = SettingsLoader.Load(new Dictionary<string, string> { ["Host"] = "shop.local" }, NoEnvironment).GetValue();

        Assert.That(settings, Is.EqualTo(new ServerSettings("shop.local", 8080, TimeSpan.FromSeconds(30), 10, Option.None)));
        Assert.That(settings.Proxy.HasValue, Is.False);
    }

    [Test]
    public void TheEnvironmentOverridesTheFileAndBlankValuesAreIgnored()
    {
        var file = new Dictionary<string, string> { ["Host"] = "shop.local", ["Port"] = "9000", ["TimeoutSeconds"] = "5" };
        var environment = new Dictionary<string, string> { ["APP_HOST"] = "prod.example.com", ["APP_PORT"] = "  " };

        var settings = SettingsLoader.Load(file, environment).GetValue();

        Assert.That(settings.Host, Is.EqualTo("prod.example.com"));
        Assert.That(settings.Port, Is.EqualTo(9000));
        Assert.That(settings.Timeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void EveryInvalidSettingIsReportedTogether()
    {
        var file = new Dictionary<string, string> { ["Port"] = "http", ["TimeoutSeconds"] = "-5" };

        var error = SettingsLoader.Load(file, NoEnvironment).GetError();

        var errors = ((ValidationErrors)error).ToDictionary();
        Assert.That(errors.Keys, Is.EquivalentTo(new[] { "Host", "Port", "TimeoutSeconds" }));
        Assert.That(errors["Port"], Is.EqualTo(new[] { "Port must be a whole number." }));
    }

    [Test]
    public void AnOutOfRangePortIsInvalidEvenThoughItParses()
    {
        var file = new Dictionary<string, string> { ["Host"] = "shop.local", ["Port"] = "70000" };

        var error = SettingsLoader.Load(file, NoEnvironment).GetError();

        Assert.That(((ValidationErrors)error).Errors.Single().MemberName, Is.EqualTo("Port"));
    }

    [Test]
    public void AProxyNeedsBothParts()
    {
        var host = new Dictionary<string, string> { ["Host"] = "a", ["ProxyHost"] = "proxy" };
        var both = new Dictionary<string, string> { ["Host"] = "a", ["ProxyHost"] = "proxy", ["ProxyPort"] = "3128" };

        Assert.That(SettingsLoader.Load(host, NoEnvironment).GetValue().Proxy.HasValue, Is.False);
        Assert.That(SettingsLoader.Load(both, NoEnvironment).GetValue().Proxy.GetValue(), Is.EqualTo("proxy:3128"));
    }

    [Test]
    public void ALenientSettingFallsBackToItsDefaultInsteadOfFailing()
    {
        var garbage = new Dictionary<string, string> { ["Host"] = "a", ["MaxConnections"] = "many" };
        var negative = new Dictionary<string, string> { ["Host"] = "a", ["MaxConnections"] = "-3" };
        var valid = new Dictionary<string, string> { ["Host"] = "a", ["MaxConnections"] = "25" };

        Assert.That(SettingsLoader.Load(garbage, NoEnvironment).GetValue().MaxConnections, Is.EqualTo(10));
        Assert.That(SettingsLoader.Load(negative, NoEnvironment).GetValue().MaxConnections, Is.EqualTo(10));
        Assert.That(SettingsLoader.Load(valid, NoEnvironment).GetValue().MaxConnections, Is.EqualTo(25));
    }
}
