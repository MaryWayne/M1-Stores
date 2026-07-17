using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace M1.Infrastructure.Persistence;

public class CartRepository(AppDbContext db) : ICartRepository
{
    public async Task<Cart> GetOrCreateAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await db.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Product)
                        .ThenInclude(p => p!.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            db.Carts.Add(cart);
            await db.SaveChangesAsync(ct);
        }

        return cart;
    }
}

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    private IQueryable<Order> WithDetails() => db.Orders
        .Include(o => o.Items)
            .ThenInclude(i => i.Variant)
        .Include(o => o.Payments)
        .Include(o => o.Shipment)
        .Include(o => o.Coupon)
        .Include(o => o.User)
        .AsSplitQuery();

    public Task<Order?> GetByNumberAsync(string orderNumber, Guid? userId = null, CancellationToken ct = default) =>
        WithDetails().FirstOrDefaultAsync(
            o => o.OrderNumber == orderNumber && (userId == null || o.UserId == userId), ct);

    public async Task<PagedResult<Order>> ListForUserAsync(Guid userId, PagingParams paging, CancellationToken ct = default)
    {
        var q = WithDetails().Where(o => o.UserId == userId).OrderByDescending(o => o.Id);
        var total = await q.CountAsync(ct);
        var items = await q.Skip(paging.Skip).Take(paging.SafePageSize).ToListAsync(ct);
        return new PagedResult<Order>(items, paging.SafePage, paging.SafePageSize, total);
    }

    public async Task<PagedResult<Order>> ListAllAsync(string? status, string? search, PagingParams paging, CancellationToken ct = default)
    {
        var q = WithDetails();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.OrderStatus>(status, true, out var s))
            q = q.Where(o => o.Status == s);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            q = q.Where(o => EF.Functions.Like(o.OrderNumber, term)
                || (o.User != null && (EF.Functions.Like(o.User.Email, term) || EF.Functions.Like(o.User.FullName, term))));
        }

        var ordered = q.OrderByDescending(o => o.Id);
        var total = await ordered.CountAsync(ct);
        var items = await ordered.Skip(paging.Skip).Take(paging.SafePageSize).ToListAsync(ct);
        return new PagedResult<Order>(items, paging.SafePage, paging.SafePageSize, total);
    }
}
