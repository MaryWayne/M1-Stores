using M1.Application.Catalog;
using M1.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace M1.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class CatalogController(CatalogService catalog) : ControllerBase
{
    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> Browse([FromQuery] ProductQuery query, CancellationToken ct) =>
        Ok(await catalog.BrowseAsync(query, ct));

    [HttpGet("products/{slug}")]
    public async Task<ActionResult<ProductDetailDto>> Detail(string slug, CancellationToken ct) =>
        Ok(await catalog.GetBySlugAsync(slug, ct));

    [HttpGet("products/compare")]
    public async Task<ActionResult<IReadOnlyList<ProductDetailDto>>> Compare([FromQuery] string ids, CancellationToken ct)
    {
        var guids = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty);
        return Ok(await catalog.CompareAsync(guids, ct));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Categories(CancellationToken ct) =>
        Ok(await catalog.GetCategoryTreeAsync(ct));

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> Brands(CancellationToken ct) =>
        Ok(await catalog.GetBrandsAsync(ct));
}
