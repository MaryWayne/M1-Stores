using M1.Domain.Common;

namespace M1.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<CartItem> Items { get; set; } = [];
}

public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }
    public Guid VariantId { get; set; }
    public ProductVariant? Variant { get; set; }
    public int Quantity { get; set; }
}

public class WishlistItem : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
}

public class Coupon : BaseEntity
{
    public required string Code { get; set; }
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderTotal { get; set; }
    public int MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsUsable(decimal orderSubtotal) =>
        IsActive
        && UsedCount < MaxUses
        && (ExpiresAt is null || DateTimeOffset.UtcNow < ExpiresAt)
        && (MinOrderTotal is null || orderSubtotal >= MinOrderTotal);

    public decimal DiscountFor(decimal subtotal) =>
        Type == CouponType.Percent
            ? Math.Round(subtotal * Value / 100m, 2)
            : Math.Min(Value, subtotal);
}
