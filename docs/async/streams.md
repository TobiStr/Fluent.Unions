# Streams

This page covers the operators on `IAsyncEnumerable<Result<T>>` and `IAsyncEnumerable<Option<T>>`.

```csharp
using Fluent.Unions;
using Fluent.Unions.Extensions;
```

## Values and Errors

`Values()` streams the values of the ok results. `Errors()` streams the exceptions of the error results. Empty results appear in neither. Both are lazy and pass the cancellation token of the enumeration on.

```csharp
IAsyncEnumerable<Result<Product>> imports = ImportProductsAsync(file);

await foreach (var product in imports.Values())
    await catalog.AddAsync(product);

await foreach (var error in imports.Errors())
    logger.LogWarning(error, "Row skipped");
```

Each loop enumerates the source on its own. A stream that can run again runs twice. A one-shot stream, such as one that reads a channel, gives its items to the first loop only, and the second loop sees nothing. To handle values and errors in one pass, see the import example below.

## Options

`Values()` also works on `IAsyncEnumerable<Option<T>>`. It streams the values and skips the options without one.

```csharp
IAsyncEnumerable<Option<Customer>> lookups = LookupAsync(ids);

await foreach (var customer in lookups.Values())
    Console.WriteLine(customer.Name);
```

There is no `Errors()` or `CombineAsync()` for options.

## CombineAsync

`CombineAsync` collects a stream like `Combine()`: all values in a `Result<IReadOnlyList<T>>`, or the first error. It stops reading the stream at the first error. An exception thrown by the stream itself becomes the error. It takes an optional `CancellationToken`.

```csharp
Result<IReadOnlyList<Product>> products = await imports.CombineAsync(cancellationToken);
```

Empty results are skipped.

## Streaming import example

An import that should keep going after bad rows, and report them at the end. Read the stream once, and split values from errors yourself:

```csharp
static async IAsyncEnumerable<Result<Product>> ImportProductsAsync(
    Channel<string> rows,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var row in rows.Reader.ReadAllAsync(ct))
        yield return Result.Try(() => ParseProduct(row));
}

static async Task<Result<int>> ImportAsync(Channel<string> rows, CancellationToken ct)
{
    var errors = new List<Exception>();
    var imported = 0;

    await foreach (var result in ImportProductsAsync(rows, ct))
    {
        switch (result)
        {
            case Product product:
                await catalog.AddAsync(product, ct);
                imported++;
                break;
            case Exception error:
                errors.Add(error);
                break;
        }
    }

    return errors.Count == 0 ? imported : new AggregateException(errors).Flatten();
}
```

If the first bad row should stop the import, use `CombineAsync` instead. If bad rows should be dropped silently, `Values()` is enough.

## Pitfalls

- `Values()` and `Errors()` each enumerate the source. A stream that can only be read once, such as a channel, gives its items to whichever loop reads them first.
- `CombineAsync` loads the whole stream into memory. For large imports, process items as they arrive.
- An exception thrown while iterating `Values()` or `Errors()` propagates from the `await foreach`. Only `CombineAsync` captures it.

## See also

- [Task pipelines](task-pipelines.md)
- [Option collections](../option/collections.md)
- [Validation](../validation/README.md)

---

Back to: [Async](README.md) · Next: [Validation](../validation/README.md)
