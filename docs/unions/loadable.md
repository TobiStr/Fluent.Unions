# Loadable

This page covers `Loadable<T>`, the state of data that is loaded asynchronously, known as "RemoteData" in Elm. It is meant for views and view models.

## The four states

A `Loadable<T>` is exactly one of these:

| State | Holds | How to create it |
| --- | --- | --- |
| not loaded | `NotLoaded` | `Loadable.NotLoaded`, or `default` |
| loading | `Loading` | `Loadable.Loading` |
| loaded | a `T` | assign the value |
| failed | an `Exception` | assign the exception |

A `default(Loadable<T>)` counts as not loaded, so a field you forgot to initialize renders as "nothing yet" and does not throw.

```csharp
using Fluent.Unions;

Loadable<Order> order = Loadable.NotLoaded;
order = Loadable.Loading;
order = new Order(Guid.NewGuid(), "A-1");
order = new TimeoutException("The shop did not answer.");
```

`Loadable.NotLoaded` and `Loadable.Loading` are values of the types `NotLoaded` and `Loading`. They convert to any `Loadable<T>`.

## Members

| Member | Behavior |
| --- | --- |
| `IsNotLoaded`, `IsLoading`, `IsLoaded`, `IsFaulted` | The current state. `IsNotLoaded` is also `true` for `default`. |
| `TryGetValue(out value)` | `true` and the value in the loaded state only. |
| `Select(v => ...)` | Projects the loaded value. Not loaded, loading and failed pass through. A projection that returns `null` gives not loaded. |
| `Match(onNotLoaded, onLoading, onLoaded, onError)` | One callback per state, in that order. Returns a value. |
| `GetValueOrDefault(fallback)` | The loaded value, or the fallback in every other state. |
| `ToOption()` | The loaded value as `Option<T>`, or `None`. |
| `result.ToLoadable()` | From a `Result<T>` or your own result type: loaded for a value, failed for an error. An error that is not an exception is held as a `ResultErrorException`. |
| `await task.ToLoadable()` | From a `Task<Result<T>>`: the same, and a faulted task counts as failed. |

```csharp
Loadable<Order> loaded = new Order(Guid.NewGuid(), "A-1");
Loadable<string> number = loaded.Select(o => o.Number);         // "A-1"
Loadable<Order> loading = Loadable.Loading;
Loadable<string> still = loading.Select(o => o.Number);        // still loading
string text = number.GetValueOrDefault("-");                     // "A-1"
Option<string> maybe = number.ToOption();                        // "A-1"

if (loaded.TryGetValue(out var order))
    Console.WriteLine(order.Number);
```

`ToLoadable` is the bridge from the rest of the library. An operation that returns a `Result<T>` becomes a `Loadable<T>` once it finishes. There is no overload for a bare `Task<T>`. Wrap it in `Result.TryAsync` first.

```csharp
Loadable<Order> fromResult = await LoadOrderAsync(id).ToLoadable();
Loadable<Order> fromEmpty = default(Result<Order>).ToLoadable(); // failed: a default result is an error
```

## A Blazor component

Set the state to loading before the call and replace it with the outcome when the call returns. The `switch` in the markup must cover all four states, or the compiler warns:

```razor
@using Fluent.Unions

@(order switch
{
    NotLoaded or null => "",
    Loading => "Loading...",
    Order o => $"Order {o.Number}",
    Exception e => $"Could not load the order: {e.Message}"
})

<button @onclick="LoadAsync">Reload</button>

@code {
    [Parameter] public Guid Id { get; set; }

    private Loadable<Order> order = Loadable.NotLoaded;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        order = Loadable.Loading;
        order = await OrderService.LoadOrderAsync(Id).ToLoadable();
    }
}
```

Blazor re-renders after the first `await` inside an event handler or `OnInitializedAsync`, so the loading state shows as soon as the call is pending. In other places, call `StateHasChanged()` after changing the field. `OrderService.LoadOrderAsync` returns a `Task<Result<Order>>`.

The `NotLoaded or null` arm covers the `NotLoaded` case and the `default` value. Leave out `or null` and the switch still compiles without a warning, but a `default` value throws `SwitchExpressionException`.

The same view with `Match` needs no `switch`, and cannot forget a state because each callback is required:

```razor
@order.Match(
    () => "",
    () => "Loading...",
    o => $"Order {o.Number}",
    e => $"Could not load the order: {e.Message}")
```

## A view model

The same state works in a view model for MAUI, WPF or any other UI that binds to properties:

```csharp
public class OrderViewModel
{
    public Loadable<Order> Order { get; private set; } = Loadable.NotLoaded;

    public async Task LoadAsync(Guid id)
    {
        Order = Loadable.Loading;
        Order = await LoadOrderAsync(id).ToLoadable();
    }

    public string Title => Order.Match(
        () => "",
        () => "Loading...",
        order => $"Order {order.Number}",
        error => $"Could not load the order: {error.Message}");
}
```

Raise your change notification after each assignment. `Loadable<T>` does not raise events.

## Exhaustive switch

Pattern matching works on the four cases directly:

```csharp
string text = loaded switch
{
    NotLoaded or null => "",
    Loading => "Loading...",
    Order o => $"Order {o.Number}",
    Exception e => $"Could not load: {e.Message}"
};
```

Without `or null`, a `default` value matches no arm and the switch throws `SwitchExpressionException`. The compiler does not warn about it.

## Pitfalls

- **Keep one field for all states.** Do not pair a `bool isLoading` with a nullable value. The union makes impossible combinations unrepresentable.
- **A reload resets the old value.** Setting `Loadable.Loading` drops the loaded value. Keep the previous value in a separate field if the view should keep showing it while it reloads.
- **A `null` projection is not loaded.** `Select(o => o.Nickname)` with a `null` nickname gives not loaded, not loaded-with-null.
- **Only a finished `Result` converts.** `ToLoadable` does not report progress. Set `Loading` yourself.
- **Exceptions are carried, not thrown.** A failed state holds the exception. Log it if you need the stack trace.

## See also

- [Your own unions](your-own-unions.md): declaring other unions.
- [Errors](../errors/README.md): the `Result<T>` that `ToLoadable` takes.
- `test/Fluent.Unions.Tests/LoadableTests.cs` in the repo: every member as a test.

---

Back to: [Unions](README.md) · Next: [Modeling with unions](modeling.md)
