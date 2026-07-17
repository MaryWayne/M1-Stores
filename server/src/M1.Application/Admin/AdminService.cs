using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Application.Shopping;
using M1.Domain;
using M1.Domain.Entities;

namespace M1.Application.Admin;

public record DashboardDto(
    decimal TotalRevenue, int TotalOrders, int PendingOrders, int TotalCustomers,
    decimal AvgOrderValue, IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<LowStockDto> LowStock, IReadOnlyList<OrderListItemDto> RecentOrders);

public record TopProductDto(string Name, int UnitsSold, decimal Revenue);
public record LowStockDto(Guid VariantId, string Product, string Variant, string Sku, int Stock);
public record SalesPointDto(string Period, decimal Revenue, int Orders);
public record UpdateOrderStatusRequest(string Status, string? TrackingNumber);
public record CustomerDto(Guid Id, string Email, string FullName, string? Phone, bool EmailVerified,
    bool IsDeactivated, int Orders, decimal TotalSpent, DateTimeOffset JoinedAt);
public record SaveCouponRequest(string Code, CouponType Type, decimal Value, decimal? MinOrderTotal,
    int MaxUses, DateTimeOffset? ExpiresAt, bool IsActive);
public record CouponDto(Guid Id, string Code, string Type, decimal Value, decimal? MinOrderTotal,
    int MaxUses, int UsedCount, DateTimeOffset? ExpiresAt, bool IsActive);

public class AdminService(
    IOrderRepository orderQueries,
    IRepository<Order> orders,
    IRepository<User> users,
    IRepository<ProductVariant> variants,
    IRepository<Product> products,
    IRepository<Shipment> shipments,
    IRepository<Coupon> coupons,
    IRepository<Notification> notifications,
    IUnitOfWork uow)
{
    private static readonly OrderStatus[] RevenueStatuses =
        [OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered];

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var all = (await orderQueries.ListAllAsync(null, null, new PagingParams(1, 100), ct)).Items;
        var revenueOrders = all.Where(o => RevenueStatuses.Contains(o.Status)).ToList();

        var topProducts = revenueOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductDto(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.LineTotal)))
            .OrderByDescending(t => t.Revenue)
            .Take(5).ToList();

        var lowStockVariants = (await variants.ListAsync(v => v.Stock <= 5, ct)).Take(8).ToList();
        var productIds = lowStockVariants.Select(v => v.ProductId).Distinct().ToList();
        var names = (await products.ListAsync(p => productIds.Contains(p.Id), ct))
            .ToDictionary(p => p.Id, p => p.Name);
        var lowStock = lowStockVariants
            .Select(v => new LowStockDto(v.Id, names.GetValueOrDefault(v.ProductId, "(retired product)"),
                v.Label, v.Sku, v.Stock))
            .ToList();

        var recent = all.OrderByDescending(o => o.CreatedAt).Take(5)
            .Select(o => new OrderListItemDto(o.Id, o.OrderNumber, o.Status.ToString(), o.Total,
                o.Currency, o.Items.Sum(i => i.Quantity), o.Items.FirstOrDefault()?.ImageUrl, o.CreatedAt))
            .ToList();

        return new DashboardDto(
            revenueOrders.Sum(o => o.Total),
            all.Count,
            all.Count(o => o.Status is OrderStatus.PendingPayment or OrderStatus.Paid or OrderStatus.Processing),
            await users.CountAsync(u => u.Role == UserRole.Customer, ct),
            revenueOrders.Count == 0 ? 0 : Math.Round(revenueOrders.Average(o => o.Total), 2),
            topProducts, lowStock, recent);
    }

    public async Task<IReadOnlyList<SalesPointDto>> GetSalesReportAsync(
        DateTimeOffset? from, DateTimeOffset? to, string groupBy, CancellationToken ct = default)
    {
        var start = from ?? DateTimeOffset.UtcNow.AddDays(-30);
        var end = to ?? DateTimeOffset.UtcNow;

        var all = (await orderQueries.ListAllAsync(null, null, new PagingParams(1, 100), ct)).Items
            .Where(o => RevenueStatuses.Contains(o.Status) && o.CreatedAt >= start && o.CreatedAt <= end);

        var format = groupBy == "month" ? "yyyy-MM" : "yyyy-MM-dd";
        return all.GroupBy(o => o.CreatedAt.ToString(format))
            .OrderBy(g => g.Key)
            .Select(g => new SalesPointDto(g.Key, g.Sum(o => o.Total), g.Count()))
            .ToList();
    }

    public Task<PagedResult<Order>> ListOrdersAsync(string? status, string? search, PagingParams paging, CancellationToken ct = default) =>
        orderQueries.ListAllAsync(status, search, paging, ct);

    public async Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var target))
            throw new DomainRuleException("Unknown order status.");

        var order = await orders.GetByIdAsync(orderId, ct) ?? throw new NotFoundException("Order not found.");
        var full = await orderQueries.GetByNumberAsync(order.OrderNumber, null, ct) ?? order;

        full.Status = target;

        if (target is OrderStatus.Shipped or OrderStatus.Delivered)
        {
            if (full.Shipment is null)
            {
                // Explicit Add: a navigation-discovered entity with a pre-set Guid key
                // would be inferred as Modified and fail with a concurrency exception.
                var shipment = new Shipment { OrderId = full.Id };
                shipments.Add(shipment);
                full.Shipment = shipment;
            }
            full.Shipment.TrackingNumber = request.TrackingNumber ?? full.Shipment.TrackingNumber;
            full.Shipment.ShippedAt ??= DateTimeOffset.UtcNow;
            if (target == OrderStatus.Delivered)
                full.Shipment.DeliveredAt ??= DateTimeOffset.UtcNow;
        }

        notifications.Add(new Notification
        {
            UserId = full.UserId,
            Type = NotificationType.OrderUpdate,
            Title = $"Order {full.OrderNumber}: {target}",
            Body = target switch
            {
                OrderStatus.Processing => "We're preparing your items for dispatch.",
                OrderStatus.Shipped => $"Your order is on the way{(full.Shipment?.TrackingNumber is { } t ? $" — tracking {t}" : "")}.",
                OrderStatus.Delivered => "Your order was delivered. Enjoy! 💛",
                _ => $"Your order status is now {target}."
            }
        });

        await uow.SaveChangesAsync(ct);
        return OrdersService.ToDto(full);
    }

    public async Task<PagedResult<CustomerDto>> ListCustomersAsync(string? search, PagingParams paging, CancellationToken ct = default)
    {
        var customers = await users.ListAsync(u => u.Role == UserRole.Customer, ct);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim().ToLowerInvariant();
            customers = customers.Where(u =>
                u.Email.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                u.FullName.Contains(t, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var allOrders = (await orderQueries.ListAllAsync(null, null, new PagingParams(1, 100), ct)).Items;
        var byUser = allOrders.GroupBy(o => o.UserId).ToDictionary(g => g.Key, g => g.ToList());

        var ordered = customers.OrderByDescending(u => u.CreatedAt).ToList();
        var page = ordered.Skip(paging.Skip).Take(paging.SafePageSize).Select(u =>
        {
            var mine = byUser.GetValueOrDefault(u.Id, []);
            return new CustomerDto(u.Id, u.Email, u.FullName, u.Phone, u.EmailVerified, u.IsDeactivated,
                mine.Count, mine.Where(o => RevenueStatuses.Contains(o.Status)).Sum(o => o.Total), u.CreatedAt);
        }).ToList();

        return new PagedResult<CustomerDto>(page, paging.SafePage, paging.SafePageSize, ordered.Count);
    }

    public async Task SetCustomerActiveAsync(Guid userId, bool active, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Customer not found.");
        user.IsDeactivated = !active;
        await uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CouponDto>> ListCouponsAsync(CancellationToken ct = default) =>
        (await coupons.ListAsync(ct: ct)).OrderByDescending(c => c.CreatedAt).Select(ToCouponDto).ToList();

    public async Task<CouponDto> SaveCouponAsync(Guid? id, SaveCouponRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        Coupon coupon;
        if (id is { } existingId)
        {
            coupon = await coupons.GetByIdAsync(existingId, ct) ?? throw new NotFoundException("Coupon not found.");
        }
        else
        {
            if (await coupons.AnyAsync(c => c.Code == code, ct))
                throw new DomainRuleException("A coupon with this code already exists.");
            coupon = new Coupon { Code = code };
            coupons.Add(coupon);
        }

        coupon.Code = code;
        coupon.Type = request.Type;
        coupon.Value = request.Value;
        coupon.MinOrderTotal = request.MinOrderTotal;
        coupon.MaxUses = request.MaxUses;
        coupon.ExpiresAt = request.ExpiresAt;
        coupon.IsActive = request.IsActive;

        await uow.SaveChangesAsync(ct);
        return ToCouponDto(coupon);
    }

    public async Task DeleteCouponAsync(Guid id, CancellationToken ct = default)
    {
        var coupon = await coupons.GetByIdAsync(id, ct) ?? throw new NotFoundException("Coupon not found.");
        coupon.IsActive = false; // keep referenced coupons; deactivate instead of delete
        await uow.SaveChangesAsync(ct);
    }

    private static CouponDto ToCouponDto(Coupon c) => new(
        c.Id, c.Code, c.Type.ToString(), c.Value, c.MinOrderTotal, c.MaxUses, c.UsedCount, c.ExpiresAt, c.IsActive);
}
