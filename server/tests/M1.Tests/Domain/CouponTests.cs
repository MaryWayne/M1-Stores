using M1.Domain;
using M1.Domain.Entities;

namespace M1.Tests.Domain;

public class CouponTests
{
    private static Coupon PercentCoupon(decimal value = 10, decimal? minOrder = null) => new()
    {
        Code = "TEST10", Type = CouponType.Percent, Value = value,
        MaxUses = 100, MinOrderTotal = minOrder
    };

    [Fact]
    public void PercentCoupon_ComputesRoundedDiscount()
    {
        var coupon = PercentCoupon(10);
        Assert.Equal(1040m, coupon.DiscountFor(10_400m));
        Assert.Equal(0.33m, coupon.DiscountFor(3.33m));
    }

    [Fact]
    public void FixedCoupon_NeverExceedsSubtotal()
    {
        var coupon = new Coupon { Code = "F500", Type = CouponType.Fixed, Value = 500, MaxUses = 10 };
        Assert.Equal(500m, coupon.DiscountFor(2_000m));
        Assert.Equal(300m, coupon.DiscountFor(300m));
    }

    [Fact]
    public void Coupon_RespectsMinimumOrderTotal()
    {
        var coupon = PercentCoupon(10, minOrder: 1_000m);
        Assert.False(coupon.IsUsable(999m));
        Assert.True(coupon.IsUsable(1_000m));
    }

    [Fact]
    public void Coupon_ExhaustedOrExpiredOrInactive_IsNotUsable()
    {
        var exhausted = PercentCoupon();
        exhausted.UsedCount = exhausted.MaxUses;
        Assert.False(exhausted.IsUsable(5_000m));

        var expired = PercentCoupon();
        expired.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        Assert.False(expired.IsUsable(5_000m));

        var inactive = PercentCoupon();
        inactive.IsActive = false;
        Assert.False(inactive.IsUsable(5_000m));
    }
}

public class OrderTests
{
    [Fact]
    public void Order_CanOnlyBeCancelledBeforeProcessing()
    {
        var order = new Order
        {
            OrderNumber = "M1-2026-000001",
            ShippingAddress = new ShippingAddress { FullName = "T", Phone = "0", Line1 = "L", City = "C", County = "K" }
        };

        order.Status = OrderStatus.PendingPayment;
        Assert.True(order.CanBeCancelled);
        order.Status = OrderStatus.Paid;
        Assert.True(order.CanBeCancelled);
        order.Status = OrderStatus.Processing;
        Assert.False(order.CanBeCancelled);
        order.Status = OrderStatus.Shipped;
        Assert.False(order.CanBeCancelled);
    }
}
