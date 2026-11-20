using System.Runtime.CompilerServices;
using Argon;

namespace Fluent.Unions.Verify;

internal sealed class UnionConverter : WriteOnlyJsonConverter
{
    public override bool CanConvert(Type type) =>
        type == typeof(Result)
        || type.IsGenericType
        && type.GetGenericTypeDefinition() is var definition
        && (definition == typeof(Result<>) || definition == typeof(Result<,>)
            || definition == typeof(Option<>) || definition == typeof(Loadable<>));

    public override void Write(VerifyJsonWriter writer, object value)
    {
        var type = value.GetType();
        var union = (IUnion)value;

        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Option<>) && type.GetGenericTypeDefinition() != typeof(Loadable<>))
            WriteResult(writer, value, union);
        else if (type.GetGenericTypeDefinition() == typeof(Option<>))
            WriteOption(writer, union);
        else
            WriteLoadable(writer, union);
    }

    private static void WriteResult(VerifyJsonWriter writer, object result, IUnion union)
    {
        var type = result.GetType();

        if ((bool)type.GetProperty("IsSuccess")!.GetValue(result)!)
        {
            if (type == typeof(Result))
            {
                writer.WriteValue("Ok");
                return;
            }

            WriteCase(writer, "Ok", union.Value);
            return;
        }

        WriteCase(writer, "Error", type.GetMethod("GetError")!.Invoke(result, null));
    }

    private static void WriteOption(VerifyJsonWriter writer, IUnion option)
    {
        if (option.Value is null or None)
            writer.WriteValue("None");
        else
            WriteCase(writer, "Some", option.Value);
    }

    private static void WriteLoadable(VerifyJsonWriter writer, IUnion loadable)
    {
        switch (loadable.Value)
        {
            case null or NotLoaded:
                writer.WriteValue("NotLoaded");
                break;
            case Loading:
                writer.WriteValue("Loading");
                break;
            case Exception error:
                WriteCase(writer, "Error", error);
                break;
            case var value:
                WriteCase(writer, "Value", value);
                break;
        }
    }

    private static void WriteCase(VerifyJsonWriter writer, string name, object? value)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(name);
        WriteData(writer, value);
        writer.WriteEndObject();
    }

    private static void WriteData(VerifyJsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNull();
                break;
            case IUnion { Value: { } inner }:
                WriteData(writer, inner);
                break;
            case ValidationErrors errors:
                WriteException(writer, errors, () =>
                {
                    writer.WritePropertyName("Errors");
                    writer.WriteStartArray();
                    foreach (var error in errors.Errors)
                        WriteData(writer, error);
                    writer.WriteEndArray();
                });
                break;
            case ValidationError error:
                WriteException(writer, error, () =>
                {
                    if (error.MemberName is not null)
                        writer.WriteMember(error, error.MemberName, "MemberName");
                });
                break;
            case Exception exception:
                WriteException(writer, exception, null);
                break;
            default:
                writer.Serialize(value);
                break;
        }
    }

    private static void WriteException(VerifyJsonWriter writer, Exception exception, Action? writeExtras)
    {
        writer.WriteStartObject();
        writer.WriteMember(exception, exception.GetType().Name, "Type");
        writer.WriteMember(exception, exception.Message, "Message");
        writeExtras?.Invoke();
        writer.WriteEndObject();
    }
}
