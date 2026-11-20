using System.Text.Json;
using System.Text.Json.Serialization;
using Fluent.Unions.Serialization;
using Fluent.Unions.Validation;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Tests.Samples;

public record LocalDisk(string Path);

public record AzureBlob(string ConnectionString, string Container);

public record S3(string Bucket, string Region);

[JsonConverter(typeof(UnionJsonConverter))]
public union StorageOptions(LocalDisk, AzureBlob, S3);

public record AppSettings(StorageOptions Storage);

public interface IBlobStorage
{
    string Location { get; }
}

public record LocalDiskStorage(string Path) : IBlobStorage
{
    public string Location => Path;
}

public record AzureBlobStorage(string ConnectionString, string Container) : IBlobStorage
{
    public string Location => $"azure://{Container}";
}

public record S3Storage(string Bucket, string Region) : IBlobStorage
{
    public string Location => $"s3://{Bucket} ({Region})";
}

public static class StorageConfiguration
{
    public static Result<StorageOptions> Check(StorageOptions options) => options switch
    {
        LocalDisk disk => Result.From(disk)
            .Validate(d => d.Path.EnsureNotWhiteSpace())
            .Select(_ => options),
        AzureBlob blob => Result.From(blob)
            .Validate(
                b => b.ConnectionString.EnsureNotWhiteSpace(),
                b => b.Container.EnsureMatches(@"^[a-z0-9-]{3,63}$"))
            .Select(_ => options),
        S3 s3 => Result.From(s3)
            .Validate(
                s => s.Bucket.EnsureNotWhiteSpace(),
                s => s.Region.EnsureMatches(@"^[a-z]{2}-[a-z]+-\d$"))
            .Select(_ => options),
        null => new ValidationError("Storage must be configured.", "Storage")
    };

    public static IBlobStorage Create(StorageOptions options) => options switch
    {
        LocalDisk disk => new LocalDiskStorage(disk.Path),
        AzureBlob blob => new AzureBlobStorage(blob.ConnectionString, blob.Container),
        S3 s3 => new S3Storage(s3.Bucket, s3.Region)
    };

    public static Result<IBlobStorage> Load(string json) =>
        Result.Try(() => JsonSerializer.Deserialize<AppSettings>(json)!)
            .Chain(settings => Check(settings.Storage))
            .Select(Create);
}

public class StorageOptionsSampleTests
{
    private static string Settings(string storage) => $$"""{ "Storage": {{storage}} }""";

    private static IEnumerable<TestCaseData> Valid()
    {
        yield return new TestCaseData(
            """{ "$type": "LocalDisk", "$value": { "Path": "/var/data" } }""",
            new LocalDiskStorage("/var/data")).SetName("Local disk");
        yield return new TestCaseData(
            """{ "$type": "AzureBlob", "$value": { "ConnectionString": "UseDevelopmentStorage=true", "Container": "uploads" } }""",
            new AzureBlobStorage("UseDevelopmentStorage=true", "uploads")).SetName("Azure blob");
        yield return new TestCaseData(
            """{ "$type": "S3", "$value": { "Bucket": "uploads", "Region": "eu-west-1" } }""",
            new S3Storage("uploads", "eu-west-1")).SetName("S3");
    }

    [TestCaseSource(nameof(Valid))]
    public void ConfiguredCaseSelectsItsImplementation(string storage, IBlobStorage expected)
    {
        var result = StorageConfiguration.Load(Settings(storage));

        Assert.That(result.GetValue(), Is.EqualTo(expected));
    }

    [Test]
    public void EveryInvalidSettingOfTheCaseIsReported()
    {
        var result = StorageConfiguration.Load(Settings("""{ "$type": "S3", "$value": { "Bucket": " ", "Region": "Ireland" } }"""));

        var errors = ((ValidationErrors)result.GetError()).Errors;
        Assert.That(errors.Select(e => e.MemberName), Is.EqualTo(new[] { "Bucket", "Region" }));
    }

    [Test]
    public void MissingStorageIsAValidationError()
    {
        var result = StorageConfiguration.Load("{ }");

        var error = (ValidationError)result.GetError();
        Assert.That(error.MemberName, Is.EqualTo("Storage"));
    }

    [Test]
    public void UnknownProviderIsAJsonError()
    {
        var result = StorageConfiguration.Load(Settings("""{ "$type": "Ftp", "$value": { } }"""));

        Assert.That(result.GetError(), Is.TypeOf<JsonException>());
        Assert.That(result.GetError().Message, Does.Contain("'Ftp' is not a case of StorageOptions."));
    }
}
