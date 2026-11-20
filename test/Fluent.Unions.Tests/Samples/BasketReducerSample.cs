using System.Collections.Immutable;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Tests.Samples;

public record BasketLine(string Sku, decimal Price, int Quantity);

public record AddProduct(string Sku, decimal Price);

public record RemoveProduct(string Sku);

public record ApplyCoupon(string Code);

public record ClearBasket;

public union BasketAction(AddProduct, RemoveProduct, ApplyCoupon, ClearBasket);

public record Basket(ImmutableList<BasketLine> Lines, string? Coupon, Loadable<decimal> Discount)
{
    public static readonly Basket Empty = new([], null, Loadable.NotLoaded);

    public decimal Total => decimal.Max(0, Lines.Sum(line => line.Price * line.Quantity) - Discount.GetValueOrDefault(0m));
}

public interface ICouponService
{
    Task<Result<decimal>> CheckAsync(string code);
}

public static class BasketReducer
{
    public static Basket Reduce(Basket basket, BasketAction action) => action switch
    {
        AddProduct add => basket with { Lines = AddLine(basket.Lines, add) },
        RemoveProduct remove => basket with { Lines = basket.Lines.RemoveAll(line => line.Sku == remove.Sku) },
        ApplyCoupon coupon => basket with { Coupon = coupon.Code, Discount = Loadable.Loading },
        ClearBasket => Basket.Empty
    };

    private static ImmutableList<BasketLine> AddLine(ImmutableList<BasketLine> lines, AddProduct add) =>
        lines.FindIndex(line => line.Sku == add.Sku) is >= 0 and var index
            ? lines.SetItem(index, lines[index] with { Quantity = lines[index].Quantity + 1 })
            : lines.Add(new BasketLine(add.Sku, add.Price, 1));

    public static string DescribeDiscount(Loadable<decimal> discount) => discount.Match(
        () => "No coupon",
        () => "Checking coupon...",
        amount => $"Discount {amount}",
        error => $"Coupon rejected: {error.Message}");
}

public sealed class BasketStore(ICouponService coupons)
{
    public Basket State { get; private set; } = Basket.Empty;

    public async Task DispatchAsync(BasketAction action)
    {
        State = BasketReducer.Reduce(State, action);
        if (action is not ApplyCoupon coupon)
            return;

        var discount = await coupons.CheckAsync(coupon.Code).ToLoadable();
        if (State.Coupon == coupon.Code)
            State = State with { Discount = discount };
    }
}

public class BasketReducerSampleTests
{
    private sealed class FakeCoupons : ICouponService
    {
        public Dictionary<string, decimal> Known { get; } = new() { ["SAVE5"] = 5m };

        public TaskCompletionSource? Gate { get; set; }

        public async Task<Result<decimal>> CheckAsync(string code)
        {
            if (Gate is not null)
                await Gate.Task;

            return Known.TryGetValue(code, out var amount) ? amount : new KeyNotFoundException($"Unknown coupon {code}.");
        }
    }

    private static Basket Fold(params BasketAction[] actions) => actions.Aggregate(Basket.Empty, BasketReducer.Reduce);

    [Test]
    public void ActionsFoldIntoTheNextBasket()
    {
        var basket = Fold(new AddProduct("TEA", 4.5m), new AddProduct("CAKE", 3m), new AddProduct("TEA", 4.5m), new RemoveProduct("CAKE"));

        Assert.That(basket.Lines, Is.EqualTo(new[] { new BasketLine("TEA", 4.5m, 2) }));
        Assert.That(basket.Total, Is.EqualTo(9m));
    }

    [Test]
    public void ClearBringsBackTheEmptyBasket()
    {
        var basket = Fold(new AddProduct("TEA", 4.5m), new ApplyCoupon("SAVE5"), new ClearBasket());

        Assert.That(basket, Is.EqualTo(Basket.Empty));
    }

    [Test]
    public void ReducingDoesNotChangeTheOldBasket()
    {
        var before = Fold(new AddProduct("TEA", 4.5m));

        BasketReducer.Reduce(before, new AddProduct("TEA", 4.5m));

        Assert.That(before.Lines.Single().Quantity, Is.EqualTo(1));
    }

    [Test]
    public void ApplyingACouponStartsLoadingWithoutAnyIo()
    {
        var basket = Fold(new ApplyCoupon("SAVE5"));

        Assert.That(basket.Discount.IsLoading, Is.True);
        Assert.That(BasketReducer.DescribeDiscount(basket.Discount), Is.EqualTo("Checking coupon..."));
    }

    [Test]
    public async Task KnownCouponLoadsItsDiscount()
    {
        var store = new BasketStore(new FakeCoupons());

        await store.DispatchAsync(new AddProduct("TEA", 4.5m));
        await store.DispatchAsync(new ApplyCoupon("SAVE5"));

        Assert.That(BasketReducer.DescribeDiscount(store.State.Discount), Is.EqualTo("Discount 5"));
        Assert.That(store.State.Total, Is.EqualTo(0m));
    }

    [Test]
    public async Task UnknownCouponFails()
    {
        var store = new BasketStore(new FakeCoupons());

        await store.DispatchAsync(new ApplyCoupon("NOPE"));

        Assert.That(store.State.Discount.IsFaulted, Is.True);
        Assert.That(BasketReducer.DescribeDiscount(store.State.Discount), Is.EqualTo("Coupon rejected: Unknown coupon NOPE."));
    }

    [Test]
    public async Task LateAnswerForAReplacedCouponIsIgnored()
    {
        var coupons = new FakeCoupons { Gate = new TaskCompletionSource() };
        var store = new BasketStore(coupons);

        var pending = store.DispatchAsync(new ApplyCoupon("SAVE5"));
        await store.DispatchAsync(new ClearBasket());
        coupons.Gate.SetResult();
        await pending;

        Assert.That(store.State, Is.EqualTo(Basket.Empty));
    }
}
