using Fluent.Unions.Grpc.Tests.Protos;
using Grpc.Core;

namespace Fluent.Unions.Grpc.Tests;

public sealed class LookupService : Lookup.LookupBase
{
    public override async Task<LookupReply> Get(LookupRequest request, ServerCallContext context) =>
        (await Find(request.Name, context.CancellationToken)).Check().GetValue();

    public override async Task<LookupReply> Upload(IAsyncStreamReader<LookupRequest> requestStream, ServerCallContext context)
    {
        await requestStream.MoveNext(context.CancellationToken);
        return (await Find(requestStream.Current.Name, context.CancellationToken)).Check().GetValue();
    }

    public override async Task Watch(LookupRequest request, IServerStreamWriter<LookupReply> responseStream, ServerCallContext context) =>
        await responseStream.WriteAsync((await Find(request.Name, context.CancellationToken)).Check().GetValue());

    public override async Task Chat(IAsyncStreamReader<LookupRequest> requestStream, IServerStreamWriter<LookupReply> responseStream, ServerCallContext context)
    {
        await requestStream.MoveNext(context.CancellationToken);
        await responseStream.WriteAsync((await Find(requestStream.Current.Name, context.CancellationToken)).Check().GetValue());
    }

    private static async Task<Result<LookupReply>> Find(string name, CancellationToken cancellationToken)
    {
        switch (name)
        {
            case "ada":
                return new LookupReply { Value = "Countess" };
            case "missing":
                return new KeyNotFoundException("No such name: missing");
            case "invalid":
                return new ValidationErrors([new ValidationError("Name is too short.", "name"), new ValidationError("Name is reserved.", "name")]);
            case "forbidden":
                return new UnauthorizedAccessException("Secret reason");
            case "rpc":
                return new RpcException(new Status(StatusCode.Aborted, "Retry later"));
            case "custom":
                return new LookupConflictException("Name is taken");
            case "slow":
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return new LookupReply();
            default:
                return new InvalidOperationException("Secret internals");
        }
    }
}

public sealed class LookupConflictException(string message) : Exception(message);
