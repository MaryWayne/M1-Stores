using M1.Application.Catalog;
using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace M1.Infrastructure.Persistence;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    private IQueryable<Product> WithDetails() => db.Products
        .Include(p => p.Category)
        .Include(p => p.Brand)
        .Include(p => p.Images)
        .Include(p => p.Variants)
        .AsSplitQuery();

    public async Task<PagedResult<Product>> SearchAsync(ProductQuery query, CancellationToken ct = default)
    {
        var q = WithDetails();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            q = q.Where(p =>
                EF.Functions.Like(p.Name, term) ||
                EF.Functions.Like(p.Description, term) ||
                (p.Brand != null && EF.Functions.Like(p.Brand.Name, term)));
        }

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
            q = q.Where(p => p.Category!.Slug == query.CategorySlug);
        if (!string.IsNullOrWhiteSpace(query.BrandSlug))
            q = q.Where(p => p.Brand!.Slug == query.BrandSlug);
        if (query.MinPrice is { } min)
            q = q.Where(p => p.BasePrice >= min);
        if (query.MaxPrice is { } max)
            q = q.Where(p => p.BasePrice <= max);
        if (query.MinRating is { } rating)
            q = q.Where(p => p.AvgRating >= rating);
        if (query.InStock == true)
            q = q.Where(p => p.Variants.Any(v => v.Stock > 0));
        if (query.Featured == true)
            q = q.Where(p => p.IsFeatured);

        q = query.Sort switch
        {
            "price-asc" => q.OrderBy(p => p.BasePrice),
            "price-desc" => q.OrderByDescending(p => p.BasePrice),
            "rating" => q.OrderByDescending(p => p.AvgRating).ThenByDescending(p => p.ReviewCount),
            "popular" => q.OrderByDescending(p => p.ReviewCount).ThenByDescending(p => p.AvgRating),
            _ => q.OrderByDescending(p => p.CreatedAt)
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip(query.Skip).Take(query.SafePageSize).ToListAsync(ct);
        return new PagedResult<Product>(items, query.SafePage, query.SafePageSize, total);
    }

    public Task<Product?> GetBySlugWithDetailsAsync(string slug, CancellationToken ct = default) =>
        WithDetails().FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        WithDetails().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<Product>> GetByIdsWithDetailsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        WithDetails().Where(p => ids.Contains(p.Id)).ToListAsync(ct);
}
