using M1.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace M1.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminCatalogController(AdminCatalogService admin) : ControllerBase
{
    [HttpPost("products")]
    public async Task<ActionResult<ProductDetailDto>> CreateProduct(SaveProductRequest request, CancellationToken ct) =>
        Ok(await admin.CreateProductAsync(request, ct));

    [HttpPut("products/{id:guid}")]
    public async Task<ActionResult<ProductDetailDto>> UpdateProduct(Guid id, SaveProductRequest request, CancellationToken ct) =>
        Ok(await admin.UpdateProductAsync(id, request, ct));

    [HttpDelete("products/{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        await admin.DeleteProductAsync(id, ct);
        return NoContent();
    }

    [HttpPost("products/{id:guid}/variants")]
    public async Task<ActionResult<VariantDto>> AddVariant(Guid id, SaveVariantRequest request, CancellationToken ct) =>
        Ok(await admin.AddVariantAsync(id, request, ct));

    [HttpPut("variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateVariant(Guid variantId, SaveVariantRequest request, CancellationToken ct)
    {
        await admin.UpdateVariantAsync(variantId, request, ct);
        return NoContent();
    }

    [HttpPost("products/{id:guid}/images")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ImageDto>> UploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await admin.AddImageAsync(id, stream, file.FileName, file.ContentType, ct));
    }

    [HttpDelete("images/{imageId:guid}")]
    public async Task<IActionResult> RemoveImage(Guid imageId, CancellationToken ct)
    {
        await admin.RemoveImageAsync(imageId, ct);
        return NoContent();
    }

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(SaveCategoryRequest request, CancellationToken ct) =>
        Ok(await admin.SaveCategoryAsync(null, request, ct));

    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, SaveCategoryRequest request, CancellationToken ct) =>
        Ok(await admin.SaveCategoryAsync(id, request, ct));

    [HttpPost("brands")]
    public async Task<ActionResult<BrandDto>> CreateBrand(SaveBrandRequest request, CancellationToken ct) =>
        Ok(await admin.SaveBrandAsync(null, request, ct));

    [HttpPut("brands/{id:guid}")]
    public async Task<ActionResult<BrandDto>> UpdateBrand(Guid id, SaveBrandRequest request, CancellationToken ct) =>
        Ok(await admin.SaveBrandAsync(id, request, ct));
}
