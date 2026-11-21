using System.Buffers;
using Fluent.Unions.Abstractions;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Serializers;
using Orleans.Serialization.WireProtocol;

namespace Fluent.Unions.Orleans;

internal sealed class ResultRejectingCodec : IGeneralizedCodec
{
    public bool IsSupportedType(Type type) => ResultTypes.IsResult(type);

    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type? expectedType, object? value)
        where TBufferWriter : IBufferWriter<byte> =>
        throw NotSupported(value?.GetType());

    public object ReadValue<TInput>(ref Reader<TInput> reader, Field field) =>
        throw NotSupported(field.FieldType ?? typeof(Result));

    internal static NotSupportedException NotSupported(Type? type) => new(
        $"{type ?? typeof(Result)} is not serialized, because an error would expose exception details. " +
        "Convert it first, for example with Match, GetValueOrDefault or ToOption.");
}
