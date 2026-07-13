using M1.Domain.Common;

namespace M1.Domain.Entities;

public class Order : BaseEntity
{
    public required string OrderNumber { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "KES";

    public Guid? CouponId { get; set; }
    public Coupon? Coupon { get; set; }

    // Snapshot of the delivery address at purchase time (owned entity):
    // later edits to the user's saved addresses must not rewrite order history.
    public required ShippingAddress ShippingAddress { get; set; }

    public List<OrderItem> Items { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public Shipment? Shipment { get; set; }

    public bool CanBeCancelled => Status is OrderStatus.PendingPayment or OrderStatus.Paid;
}

/// <summary>Owned by Order — persisted as columns on the Orders table.</summary>
public class ShippingAddress
{
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public required string Line1 { get; set; }
    public required string City { get; set; }
    public required string County { get; set; }
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    // Snapshots: the receipt must not change when the catalog does.
    public required string ProductName { get; set; }
    public string VariantLabel { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public PaymentProvider Provider { get; set; }
    public string? ProviderRef { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}

public class Shipment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public string Carrier { get; set; } = "M1 Delivery";
    public string? TrackingNumber { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
}
