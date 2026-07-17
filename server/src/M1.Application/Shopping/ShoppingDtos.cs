using M1.Domain;
using M1.Domain.Entities;

namespace M1.Application.Shopping;

/// <summary>Store-level commerce settings, bound from configuration at startup.</summary>
public class StoreOptions
{
    public decimal ShippingFee { get; set; } = 300m;
    public decimal FreeShippingThreshold { get; set; } = 10_000m;
}

// ---- Cart ----
public record AddCartItemRequest(Guid VariantId, int Quantity);
public record UpdateCartItemRequest(int Quantity);
public record MergeCartRequest(List<AddCartItemRequest> Items);

public record CartItemDto(
    Guid Id, Guid VariantId, Guid ProductId, string ProductName, string ProductSlug,
    string VariantLabel, string? ImageUrl, decimal UnitPrice, int Quantity,
    decimal LineTotal, int StockAvailable);

public record CartDto(Guid Id, IReadOnlyList<CartItemDto> Items, decimal Subtotal, int ItemCount);

// ---- Wishlist ----
public record WishlistItemDto(Guid ProductId, string Name, string Slug, decimal Price,
    string? ImageUrl, double AvgRating, bool InStock);

// ---- Checkout ----
public record QuoteRequest(string? CouponCode);
public record QuoteDto(decimal Subtotal, decimal DiscountAmount, decimal ShippingFee,
    decimal Total, string? CouponCode, string? CouponError);

public record CheckoutRequest(Guid AddressId, string? CouponCode, PaymentProvider PaymentProvider, string? MpesaPhone);

// ---- Addresses ----
public record SaveAddressRequest(string Label, string FullName, string Phone,
    string Line1, string City, string County, bool IsDefault);
public record AddressDto(Guid Id, string Label, string FullName, string Phone,
    string Line1, string City, string County, bool IsDefault);

// ---- Orders ----
public record OrderItemDto(string ProductName, string VariantLabel, string? ImageUrl,
    decimal UnitPrice, int Quantity, decimal LineTotal);

public record OrderTimelineEntry(string Status, DateTimeOffset? At);

public record OrderDto(
    Guid Id, string OrderNumber, string Status,
    decimal Subtotal, decimal DiscountAmount, decimal ShippingFee, decimal Total, string Currency,
    string? CouponCode, string PaymentProvider, string PaymentStatus,
    ShippingAddress ShippingAddress, IReadOnlyList<OrderItemDto> Items,
    string? TrackingNumber, DateTimeOffset PlacedAt, bool CanBeCancelled,
    IReadOnlyList<OrderTimelineEntry> Timeline);

public record OrderListItemDto(
    Guid Id, string OrderNumber, string Status, decimal Total, string Currency,
    int ItemCount, string? FirstImageUrl, DateTimeOffset PlacedAt);

// ---- Notifications ----
public record NotificationDto(Guid Id, string Type, string Title, string Body, bool IsRead, DateTimeOffset CreatedAt);
