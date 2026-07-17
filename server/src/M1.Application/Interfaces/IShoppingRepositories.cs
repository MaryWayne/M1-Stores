using M1.Application.Common;
using M1.Domain.Entities;

namespace M1.Application.Interfaces;

public interface ICartRepository
{
    /// <summary>Loads the user's cart with items → variant → product → images, creating it if missing.</summary>
    Task<Cart> GetOrCreateAsync(Guid userId, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByNumberAsync(string orderNumber, Guid? userId = null, CancellationToken ct = default);
    Task<PagedResult<Order>> ListForUserAsync(Guid userId, PagingParams paging, CancellationToken ct = default);
    Task<PagedResult<Order>> ListAllAsync(string? status, string? search, PagingParams paging, CancellationToken ct = default);
}
