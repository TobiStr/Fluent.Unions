using System.Text.Json;
using AotSample;
using Fluent.Unions;
using Fluent.Unions.Abstractions;

var failures = 0;

#if TRIMMED_ONLY
Fluent.Unions.Serialization.UnionCaseNames.For(typeof(Shape));
#endif

void Check(bool passed, string name)
{
    Console.WriteLine($"{(passed ? "ok    " : "FAILED")} {name}");
    if (!passed)
        failures++;
}

bool Throws<TException>(Action action, Func<TException, bool>? matches = null) where TException : Exception
{
    try
    {
        action();
        return false;
    }
    catch (TException exception)
    {
        return matches?.Invoke(exception) ?? true;
    }
}

Result<int> parsed = Result.Try(() => int.Parse("42"));
Check(parsed.Select(value => value + 1).GetValue() == 43, "Result: Select");
Check(parsed.Chain(value => value > 0 ? Result<int>.Ok(value) : new ArgumentException("negative")).IsSuccess, "Result: Chain");
Check(Result.Try(() => int.Parse("x")).TryGetError(out var parseError) && parseError is FormatException, "Result: Try captures");
Check(Result<int>.Error(new TimeoutException()).Catch(_ => Result<int>.Ok(1)).GetValue() == 1, "Result: Catch");
Check(parsed.Chain(value => Result<int>.Ok(value), (a, b) => a + b).GetValue() == 84, "Result: Chain(step, combine)");
Check(parsed.Match(value => value, _ => -1) == 42, "Result: Match");

var piped = await Task.FromResult(parsed)
    .Select(value => value * 2)
    .ChainAwait(async value =>
    {
        await Task.Yield();
        return Result<int>.Ok(value + 1);
    });
Check(piped.GetValue() == 85, "Result: Task pipeline");

var observed = 0;
Check(parsed.Tap(value => observed = value).GetValue() == 42 && observed == 42, "Result: Tap");
Check(Throws<TimeoutException>(() => parsed.Tap(_ => throw new TimeoutException())), "Result: Tap propagates");

Option<string> name = Option.Some("Ada");
Check(name.Select(text => text.Length).GetValueOrDefault(0) == 3, "Option: Select");
Option<int> none = Option.None;
Check(!none.HasValue, "Option: None");

DomainResult<int> found = 5;
Check(found.Select(_ => Fail(new NotFoundException("missing"))).GetError() is NotFoundException, "Declared type: captures a declared exception");
Check(Throws<NullReferenceException>(() => found.Select(_ => Fail(new NullReferenceException()))), "Declared type: undeclared exception escapes");

DomainResult<int> missing = new NotFoundException("No order 7.");
var api = missing.MapError<ApiResult<int>, ApiError>(error => new BadRequest(error.Message));
Check(api.TryGetError(out var apiError) && apiError.Value is BadRequest { Message: "No order 7." }, "Declared type: MapError");

Check(ResultTypes.IsResult(typeof(DomainResult<int>)), "ResultTypes: declared type");
Check(ResultTypes.ErrorType(typeof(Result<int>)) == typeof(Exception), "ResultTypes: error type");
Check(ResultTypes.ValueType(typeof(ApiResult<string>)) == typeof(string), "ResultTypes: value type");

var customerJson = JsonSerializer.Serialize(new CustomerDto("Ada", Option.None, 36), AppJsonContext.Default.CustomerDto);
Check(customerJson == """{"Name":"Ada","Nickname":null,"Age":36}""", "JSON: options with OptionJsonConverter<T>");
var customer = JsonSerializer.Deserialize("""{"Name":"Ada","Nickname":"Countess","Age":null}""", AppJsonContext.Default.CustomerDto)!;
Check(customer.Nickname.GetValueOrDefault("") == "Countess" && !customer.Age.HasValue, "JSON: reads options");

var drawingJson = JsonSerializer.Serialize(new Drawing("Sketch", new Circle(2)), AppJsonContext.Default.Drawing);
Check(drawingJson == """{"Title":"Sketch","Shape":{"$type":"circle","$value":{"Radius":2}}}""", "JSON: union with UnionJsonConverter<T>");
var drawing = JsonSerializer.Deserialize("""{"Title":"Sketch","Shape":{"$type":"Square","$value":{"Side":3}}}""", AppJsonContext.Default.Drawing)!;
Check(drawing.Shape.Value is Square { Side: 3 }, "JSON: reads a union");

Check(Throws<NotSupportedException>(() => JsonSerializer.Serialize(Result<int>.Ok(1), typeof(Result<int>), AppJsonContext.Default)), "JSON: results refuse to serialize");

string? unregistered = null;
var unregisteredFailedWithGuidance = Throws<NotSupportedException>(
    () => unregistered = JsonSerializer.Serialize(Option.Some(5L), typeof(Option<long>), AppJsonContext.Default),
    exception => exception.Message.Contains("OptionJsonConverter<Int64>"));
Check(unregistered == "5" || unregisteredFailedWithGuidance, "JSON: unregistered Option<T> works or names the converter to register");
Console.WriteLine($"       unregistered Option<long>: {(unregistered is null ? "needs OptionJsonConverter<long>" : "works")}");

Console.WriteLine(failures == 0 ? "All checks passed." : $"{failures} checks failed.");
return failures == 0 ? 0 : 1;

static int Fail(Exception exception) => throw exception;

