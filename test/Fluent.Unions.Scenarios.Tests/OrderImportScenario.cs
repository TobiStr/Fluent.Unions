using System.Globalization;
using System.Runtime.CompilerServices;
using Fluent.Unions.Validation;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Scenarios.Tests.OrderImport;

public record OrderLine(string Sku, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

public record ImportReport(IReadOnlyList<OrderLine> Imported, IReadOnlyList<string> Rejected)
{
    public decimal Total => Imported.Sum(line => line.Total);
}

public class LineRejectedException(int number, Exception reason)
    : Exception($"Line {number}: {Describe(reason)}", reason)
{
    private static string Describe(Exception reason) => reason is ValidationErrors errors
        ? string.Join(" ", errors.Errors.Select(error => error.Message))
        : reason.Message;
}

/// <summary>
/// Imports order lines from CSV text. Shows errors as values and collecting validation errors: every problem of a
/// line is reported at once, and one bad line never stops the import or hides the good lines.
/// </summary>
public static class OrderImport
{
    public static Result<OrderLine> ParseLine(string line, int number) =>
        Result.From(line.Split(';', StringSplitOptions.TrimEntries))
            .Ensure(fields => fields.Length == 3, "A line needs a SKU, a quantity and a price.")
            .Chain(fields => Result.CombineAll(
                fields[0].EnsureNotWhiteSpace(name: "Sku"),
                Result.Try(() => int.Parse(fields[1], CultureInfo.InvariantCulture))
                    .Catch((FormatException _) => new ValidationError("Quantity must be a whole number.", "Quantity"))
                    .EnsureInRange(1, 100, name: "Quantity"),
                Result.Try(() => decimal.Parse(fields[2], CultureInfo.InvariantCulture))
                    .Catch((FormatException _) => new ValidationError("UnitPrice must be a number.", "UnitPrice"))
                    .EnsurePositive(name: "UnitPrice")))
            .Select(parsed => new OrderLine(parsed.Item1, parsed.Item2, parsed.Item3))
            .Catch(error => new LineRejectedException(number, error));

    public static ImportReport Import(IEnumerable<string> lines)
    {
        var (imported, errors) = lines.Select((line, index) => ParseLine(line, index + 1)).Partition();
        return new ImportReport(imported, errors.Select(error => error.Message).ToList());
    }

    public static Result<IReadOnlyList<OrderLine>> ImportAllOrNothing(IEnumerable<string> lines) =>
        lines.Select((line, index) => ParseLine(line, index + 1)).CombineAll();

    public static async Task<ImportReport> ImportAsync(IAsyncEnumerable<string> lines, CancellationToken cancellationToken = default)
    {
        List<OrderLine> imported = [];
        List<string> rejected = [];
        var number = 0;

        await foreach (var line in lines.WithCancellation(cancellationToken))
            ParseLine(line, ++number).Do(imported.Add, error => rejected.Add(error.Message));

        return new ImportReport(imported, rejected);
    }
}

public class OrderImportScenarioTests
{
    private static readonly string[] Csv = ["TEA;2;4.50", "CAKE;1;3", ";0;-1", "TEA;two;4.50", "TEA;1"];

    [Test]
    public void AValidLineBecomesAnOrderLine()
    {
        var line = OrderImport.ParseLine("TEA; 2 ;4.50", 1);

        Assert.That(line.GetValue(), Is.EqualTo(new OrderLine("TEA", 2, 4.5m)));
        Assert.That(line.GetValue().Total, Is.EqualTo(9m));
    }

    [Test]
    public void EveryProblemOfALineIsReportedAtOnce()
    {
        var error = OrderImport.ParseLine(";0;-1", 3).GetError();

        var inner = (ValidationErrors)error.InnerException!;
        Assert.That(error, Is.TypeOf<LineRejectedException>());
        Assert.That(inner.Errors.Select(e => e.MemberName), Is.EquivalentTo(new[] { "Sku", "Quantity", "UnitPrice" }));
        Assert.That(error.Message, Does.StartWith("Line 3: Sku must not be empty or white space."));
    }

    [Test]
    public void ParseFailuresAreRejectionsAndNotExceptions()
    {
        var wrongFormat = OrderImport.ParseLine("TEA;two;4.50", 4).GetError();
        var missingFields = OrderImport.ParseLine("TEA;1", 5).GetError();

        Assert.That(wrongFormat.Message, Is.EqualTo("Line 4: Quantity must be a whole number."));
        Assert.That(missingFields.Message, Is.EqualTo("Line 5: A line needs a SKU, a quantity and a price."));
    }

    [Test]
    public void ImportKeepsGoodLinesAndReportsTheRest()
    {
        var report = OrderImport.Import(Csv);

        Assert.That(report.Imported.Select(line => line.Sku), Is.EqualTo(new[] { "TEA", "CAKE" }));
        Assert.That(report.Total, Is.EqualTo(12m));
        Assert.That(report.Rejected, Has.Count.EqualTo(3));
        Assert.That(report.Rejected[2], Does.StartWith("Line 5:"));
    }

    [Test]
    public void AllOrNothingCollectsEveryRejectedLine()
    {
        var strict = OrderImport.ImportAllOrNothing(Csv);
        var clean = OrderImport.ImportAllOrNothing(Csv.Take(2));

        var aggregate = (AggregateException)strict.GetError();
        Assert.That(aggregate.InnerExceptions, Has.Count.EqualTo(3));
        Assert.That(clean.GetValue(), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task AStreamIsImportedLineByLine()
    {
        var report = await OrderImport.ImportAsync(Stream(Csv));

        Assert.That(report.Imported, Has.Count.EqualTo(2));
        Assert.That(report.Rejected.Select(message => message[..6]), Is.EqualTo(new[] { "Line 3", "Line 4", "Line 5" }));
    }

    [Test]
    public void CancellingAStreamImportThrowsInsteadOfBecomingAnError()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.CatchAsync<OperationCanceledException>(() => OrderImport.ImportAsync(Stream(Csv), cancellation.Token));
    }

    private static async IAsyncEnumerable<string> Stream(IEnumerable<string> lines, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var line in lines)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return line;
        }
    }
}
