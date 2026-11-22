using System.Text.Json.Serialization;
using Fluent.Unions;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Serialization;

namespace AotSample;

public class DomainException(string message, Exception? inner = null) : Exception(message, inner), IFromException<DomainException>
{
    public static DomainException FromException(Exception exception) => exception as DomainException ?? new DomainException(exception.Message, exception);
}

public sealed class NotFoundException(string message) : DomainException(message);

[FluentResult<DomainException>(Capture = ExceptionCapture.Declared)]
public readonly partial struct DomainResult<T>;

public sealed record BadRequest(string Message);

public sealed record ServerError;

public union ApiError(BadRequest, ServerError) : IFromException<ApiError>
{
    public static ApiError FromException(Exception exception) => new ServerError();
}

[FluentResult<ApiError>]
public readonly partial struct ApiResult<T>;

public sealed record Circle(double Radius);

public sealed record Square(double Side);

[JsonConverter(typeof(UnionJsonConverter<Shape>))]
[UnionCaseName(typeof(Circle), "circle")]
public union Shape(Circle, Square);

public sealed record CustomerDto(string Name, Option<string> Nickname, Option<int> Age);

public sealed record Drawing(string Title, Shape Shape);

[JsonSourceGenerationOptions(Converters = [typeof(OptionJsonConverter<int>), typeof(OptionJsonConverter<string>)])]
[JsonSerializable(typeof(CustomerDto))]
[JsonSerializable(typeof(Drawing))]
[JsonSerializable(typeof(Result<int>))]
[JsonSerializable(typeof(Option<long>))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Circle))]
[JsonSerializable(typeof(Square))]
public partial class AppJsonContext : JsonSerializerContext;
