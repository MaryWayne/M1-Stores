using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain.Entities;

namespace M1.Application.Catalog;

public class CatalogService(
    IProductRepository products,
    IRepository<Category> categories,
    IRepository<Brand> brands)
{
    public async Task<PagedResult<ProductListItemDto>> BrowseAsync(ProductQuery query, CancellationToken ct = default)
    {
        var page = await products.SearchAsync(query, ct);
        return new PagedResult<ProductListItemDto>(
            page.Items.Select(CatalogMappings.ToListItem).ToList(),
            page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<ProductDetailDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var product = await products.GetBySlugWithDetailsAsync(slug, ct)
            ?? throw new NotFoundException("Product not found.");
        return CatalogMappings.ToDetail(product);
    }

    public async Task<IReadOnlyList<ProductDetailDto>> CompareAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var list = await products.GetByIdsWithDetailsAsync(ids.Distinct().Take(4), ct);
        return list.Select(CatalogMappings.ToDetail).ToList();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoryTreeAsync(CancellationToken ct = default)
    {
        var all = await categories.ListAsync(ct: ct);
        return all.Where(c => c.ParentId is null)
            .OrderBy(c => c.Name)
            .Select(c => ToDto(c, all))
            .ToList();

        static CategoryDto ToDto(Category c, List<Category> all) => new(
            c.Id, c.Name, c.Slug,
            all.Where(x => x.ParentId == c.Id).OrderBy(x => x.Name).Select(x => ToDto(x, all)).ToList());
    }

    public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(CancellationToken ct = default)
    {
        var all = await brands.ListAsync(ct: ct);
        return all.OrderBy(b => b.Name).Select(b => new BrandDto(b.Id, b.Name, b.Slug)).ToList();
    }
}
