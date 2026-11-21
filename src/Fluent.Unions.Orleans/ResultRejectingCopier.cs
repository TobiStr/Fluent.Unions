using Orleans.Serialization.Cloning;

namespace Fluent.Unions.Orleans;

internal sealed class ResultRejectingCopier<T> : IDeepCopier<Result<T>>
{
    public Result<T> DeepCopy(Result<T> input, CopyContext context) => throw ResultRejectingCodec.NotSupported(typeof(Result<T>));
}

internal sealed class ResultRejectingCopier : IDeepCopier<Result>
{
    public Result DeepCopy(Result input, CopyContext context) => throw ResultRejectingCodec.NotSupported(typeof(Result));
}
