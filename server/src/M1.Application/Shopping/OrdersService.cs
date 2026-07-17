using System.Security.Cryptography;
using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain;
using M1.Domain.Entities;

namespace M1.Application.Shopping;

public class OrdersService(
    ICartRepository carts,
    IOrderRepository orders,
    IRepository<Order> orderWrites,
    IRepository<Coupon> coupons,
    IRepository<Address> addresses,
    IRepository<CartItem> cartItems,
    IRepository<Notification> notifications,
    IEnumerable<IPaymentGateway> gateways,
    IEmailService email,
    IUnitOfWork uow,
    StoreOptions store)
{
    private decimal ShippingFee => store.ShippingFee;
    private decimal FreeShippingThreshold => store.FreeShippingThreshold;

    public async Task<QuoteDto> QuoteAsync(Guid userId, QuoteRequest request, CancellationToken ct = default)
    {
        var cart = await carts.GetOrCreateAsync(userId, ct);
        var dto = CartService.ToDto(cart);
        if (dto.Items.Count == 0) throw new DomainRuleException("Your cart is empty.");

        var (discount, couponError) = await ResolveCouponAsync(request.CouponCode, dto.Subtotal, ct);
        var shipping = dto.Subtotal - discount >= FreeShippingThreshold ? 0m : ShippingFee;

        return new QuoteDto(dto.Subtotal, discount, shipping, dto.Subtotal - discount + shipping,
            couponError is null ? request.CouponCode?.ToUpperInvariant() : null, couponError);
    }

    public async Task<(OrderDto Order, string? RedirectUrl)> CheckoutAsync(Guid userId, CheckoutRequest request, CancellationToken ct = default)
    {
        var address = await addresses.FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId, ct)
            ?? throw new NotFoundException("Delivery address not found.");

        var cart = await carts.GetOrCreateAsync(userId, ct);
        if (cart.Items.Count == 0) throw new DomainRuleException("Your cart is empty.");

        Order order = null!;
        string? redirect = null;

        await uow.ExecuteInTransactionAsync(async innerCt =>
        {
            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                var variant = item.Variant ?? throw new DomainRuleException("A cart item is no longer available.");
                var product = variant.Product!;
                if (variant.Stock < item.Quantity)
                    throw new DomainRuleException($"Only {variant.Stock} left of {product.Name} ({variant.Label}).");

                variant.Stock -= item.Quantity;
                var price = variant.PriceOverride ?? product.BasePrice;
                subtotal += price * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    VariantId = variant.Id,
                    ProductName = product.Name,
                    VariantLabel = variant.Label,
                    ImageUrl = product.Images.OrderByDescending(i => i.IsPrimary).FirstOrDefault()?.Url,
                    UnitPrice = price,
                    Quantity = item.Quantity,
                    LineTotal = price * item.Quantity
                });
            }

            Coupon? coupon = null;
            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var code = request.CouponCode.Trim().ToUpperInvariant();
                coupon = await coupons.FirstOrDefaultAsync(c => c.Code == code, innerCt);
                if (coupon is null || !coupon.IsUsable(subtotal))
                    throw new DomainRuleException("This coupon can't be applied to your order.");
                discount = coupon.DiscountFor(subtotal);
                coupon.UsedCount++;
            }

            var shipping = subtotal - discount >= FreeShippingThreshold ? 0m : ShippingFee;

            order = new Order
            {
                OrderNumber = await GenerateOrderNumberAsync(innerCt),
                UserId = userId,
                Subtotal = subtotal,
                DiscountAmount = discount,
                ShippingFee = shipping,
                Total = subtotal - discount + shipping,
                CouponId = coupon?.Id,
                ShippingAddress = new ShippingAddress
                {
                    FullName = address.FullName,
                    Phone = address.Phone,
                    Line1 = address.Line1,
                    City = address.City,
                    County = address.County
                },
                Items = orderItems
            };
            orderWrites.Add(order);

            // Payment
            var gateway = gateways.FirstOrDefault(g => g.Provider == request.PaymentProvider)
                ?? throw new DomainRuleException("Unsupported payment method.");
            var initiation = await gateway.InitiateAsync(order, request.MpesaPhone, innerCt);
            redirect = initiation.RedirectUrl;

            order.Payments.Add(new Payment
            {
                Provider = request.PaymentProvider,
                ProviderRef = initiation.ProviderRef,
                Amount = order.Total,
                Status = initiation.AutoSucceeded ? PaymentStatus.Succeeded : PaymentStatus.Pending
            });
            if (initiation.AutoSucceeded) order.Status = OrderStatus.Paid;

            foreach (var item in cart.Items.ToList()) cartItems.Remove(item);

            notifications.Add(new Notification
            {
                UserId = userId,
                Type = NotificationType.OrderUpdate,
                Title = $"Order {order.OrderNumber} placed 🎉",
                Body = initiation.AutoSucceeded
                    ? $"Payment of KES {order.Total:N0} received. We're preparing your order."
                    : $"Complete payment of KES {order.Total:N0} to confirm your order."
            });

            await uow.SaveChangesAsync(innerCt);
        }, ct);

        _ = SendOrderEmailAsync(userId, order, ct);
        var full = await orders.GetByNumberAsync(order.OrderNumber, userId, ct);
        return (ToDto(full!), redirect);
    }

    public async Task<PagedResult<OrderListItemDto>> ListMineAsync(Guid userId, PagingParams paging, CancellationToken ct = default)
    {
        var page = await orders.ListForUserAsync(userId, paging, ct);
        return new PagedResult<OrderListItemDto>(
            page.Items.Select(o => new OrderListItemDto(
                o.Id, o.OrderNumber, o.Status.ToString(), o.Total, o.Currency,
                o.Items.Sum(i => i.Quantity), o.Items.FirstOrDefault()?.ImageUrl, o.CreatedAt)).ToList(),
            page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<OrderDto> GetMineAsync(Guid userId, string orderNumber, CancellationToken ct = default)
    {
        var order = await orders.GetByNumberAsync(orderNumber, userId, ct)
            ?? throw new NotFoundException("Order not found.");
        return ToDto(order);
    }

    public async Task<OrderDto> CancelAsync(Guid userId, string orderNumber, CancellationToken ct = default)
    {
        var order = await orders.GetByNumberAsync(orderNumber, userId, ct)
            ?? throw new NotFoundException("Order not found.");
        if (!order.CanBeCancelled)
            throw new DomainRuleException("This order can no longer be cancelled.");

        await uow.ExecuteInTransactionAsync(async innerCt =>
        {
            foreach (var item in order.Items.Where(i => i.Variant is not null))
                item.Variant!.Stock += item.Quantity;

            var wasPaid = order.Status == OrderStatus.Paid;
            order.Status = wasPaid ? OrderStatus.Refunded : OrderStatus.Cancelled;
            foreach (var p in order.Payments.Where(p => p.Status == PaymentStatus.Succeeded))
                p.Status = PaymentStatus.Refunded;

            notifications.Add(new Notification
            {
                UserId = userId,
                Type = NotificationType.OrderUpdate,
                Title = $"Order {order.OrderNumber} {(wasPaid ? "refunded" : "cancelled")}",
                Body = wasPaid ? $"KES {order.Total:N0} will be refunded to your payment method." : "Your order was cancelled."
            });

            await uow.SaveChangesAsync(innerCt);
        }, ct);

        return ToDto(order);
    }

    public async Task<string> InvoiceHtmlAsync(Guid userId, string orderNumber, CancellationToken ct = default)
    {
        var order = await orders.GetByNumberAsync(orderNumber, userId, ct)
            ?? throw new NotFoundException("Order not found.");

        var rows = string.Join("", order.Items.Select(i =>
            $"<tr><td>{i.ProductName} {i.VariantLabel}</td><td>{i.Quantity}</td><td style='text-align:right'>KES {i.UnitPrice:N0}</td><td style='text-align:right'>KES {i.LineTotal:N0}</td></tr>"));

        return $$"""
            <!doctype html><html><head><meta charset="utf-8"><title>Invoice {{order.OrderNumber}}</title>
            <style>body{font-family:sans-serif;max-width:640px;margin:32px auto;color:#111}
            table{width:100%;border-collapse:collapse}td,th{padding:8px;border-bottom:1px solid #eee;text-align:left}
            .tot td{font-weight:bold;border-top:2px solid #111}h1{color:#e11d48}</style></head><body>
            <h1>M1 Stores</h1>
            <p><strong>Invoice {{order.OrderNumber}}</strong><br>Date: {{order.CreatedAt:dd MMM yyyy}}<br>
            Status: {{order.Status}}<br>Deliver to: {{order.ShippingAddress.FullName}}, {{order.ShippingAddress.Line1}}, {{order.ShippingAddress.City}}, {{order.ShippingAddress.County}} ({{order.ShippingAddress.Phone}})</p>
            <table><tr><th>Item</th><th>Qty</th><th style='text-align:right'>Price</th><th style='text-align:right'>Total</th></tr>
            {{rows}}
            <tr><td colspan="3">Subtotal</td><td style='text-align:right'>KES {{order.Subtotal:N0}}</td></tr>
            <tr><td colspan="3">Discount</td><td style='text-align:right'>- KES {{order.DiscountAmount:N0}}</td></tr>
            <tr><td colspan="3">Shipping</td><td style='text-align:right'>KES {{order.ShippingFee:N0}}</td></tr>
            <tr class="tot"><td colspan="3">Total</td><td style='text-align:right'>KES {{order.Total:N0}}</td></tr></table>
            <p style="color:#777;font-size:13px">Thank you for shopping with M1 Stores · m1stores.com</p>
            </body></html>
            """;
    }

    // ---- helpers ----

    private async Task<(decimal Discount, string? Error)> ResolveCouponAsync(string? code, decimal subtotal, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return (0, null);
        var coupon = await coupons.FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant(), ct);
        if (coupon is null) return (0, "Coupon code not found.");
        if (!coupon.IsUsable(subtotal)) return (0, "This coupon can't be applied to your order.");
        return (coupon.DiscountFor(subtotal), null);
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken ct)
    {
        while (true)
        {
            var candidate = $"M1-{DateTimeOffset.UtcNow.Year}-{RandomNumberGenerator.GetInt32(100000, 999999)}";
            if (!await orderWrites.AnyAsync(o => o.OrderNumber == candidate, ct))
                return candidate;
        }
    }

    private async Task SendOrderEmailAsync(Guid userId, Order order, CancellationToken ct)
    {
        try
        {
            var user = order.User;
            if (user is null) return;
            await email.SendAsync(user.Email, $"Your M1 Stores order {order.OrderNumber}",
                $"<p>Hi {user.FullName},</p><p>Thanks for your order <strong>{order.OrderNumber}</strong> " +
                $"totalling <strong>KES {order.Total:N0}</strong>. We'll notify you as it progresses.</p>", ct);
        }
        catch { /* email failures must never break checkout */ }
    }

    public static OrderDto ToDto(Order o)
    {
        var payment = o.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        var timeline = new List<OrderTimelineEntry>
        {
            new("Placed", o.CreatedAt),
            new("Paid", o.Status >= OrderStatus.Paid && o.Status != OrderStatus.Cancelled
                ? payment?.CreatedAt ?? o.CreatedAt : null),
            new("Shipped", o.Shipment?.ShippedAt),
            new("Delivered", o.Shipment?.DeliveredAt)
        };
        if (o.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
            timeline.Add(new(o.Status.ToString(), null));

        return new OrderDto(
            o.Id, o.OrderNumber, o.Status.ToString(),
            o.Subtotal, o.DiscountAmount, o.ShippingFee, o.Total, o.Currency,
            o.Coupon?.Code, (payment?.Provider ?? PaymentProvider.Fake).ToString(),
            (payment?.Status ?? PaymentStatus.Pending).ToString(),
            o.ShippingAddress,
            o.Items.Select(i => new OrderItemDto(i.ProductName, i.VariantLabel, i.ImageUrl, i.UnitPrice, i.Quantity, i.LineTotal)).ToList(),
            o.Shipment?.TrackingNumber, o.CreatedAt, o.CanBeCancelled, timeline);
    }
}
