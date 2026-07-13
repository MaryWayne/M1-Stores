using M1.Application.Catalog;
using M1.Application.Common;
using M1.Domain.Entities;

namespace M1.Application.Interfaces;

/// <summary>Query-heavy catalog reads that the generic repository can't express.</summary>
public interface IProductRepository
{
    Task<PagedResult<Product>> SearchAsync(ProductQuery query, CancellationToken ct = default);
    Task<Product?> GetBySlugWithDetailsAsync(string slug, CancellationToken ct = default);
    Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<List<Product>> GetByIdsWithDetailsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
