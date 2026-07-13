using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain.Entities;

namespace M1.Application.Catalog;

public record SaveProductRequest(
    string Name, string Description, Guid CategoryId, Guid? BrandId,
    decimal BasePrice, bool IsFeatured);

public record SaveVariantRequest(string? Size, string? Color, decimal? PriceOverride, int Stock);

public record SaveCategoryRequest(string Name, Guid? ParentId);
public record SaveBrandRequest(string Name, string? LogoUrl);

public class AdminCatalogService(
    IProductRepository productQueries,
    IRepository<Product> products,
    IRepository<ProductVariant> variants,
    IRepository<ProductImage> images,
    IRepository<Category> categories,
    IRepository<Brand> brands,
    IImageStorage imageStorage,
    IUnitOfWork uow)
{
    public async Task<ProductDetailDto> CreateProductAsync(SaveProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Slug = await UniqueSlugAsync(request.Name, ct),
            Description = request.Description,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            BasePrice = request.BasePrice,
            IsFeatured = request.IsFeatured
        };
        products.Add(product);
        await uow.SaveChangesAsync(ct);
        return CatalogMappings.ToDetail((await productQueries.GetByIdWithDetailsAsync(product.Id, ct))!);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(Guid id, SaveProductRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product not found.");
        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.BasePrice = request.BasePrice;
        product.IsFeatured = request.IsFeatured;
        await uow.SaveChangesAsync(ct);
        return CatalogMappings.ToDetail((await productQueries.GetByIdWithDetailsAsync(id, ct))!);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product not found.");
        product.IsDeleted = true; // soft delete: order history stays intact
        await uow.SaveChangesAsync(ct);
    }

    public async Task<VariantDto> AddVariantAsync(Guid productId, SaveVariantRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(productId, ct) ?? throw new NotFoundException("Product not found.");
        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Sku = $"M1-{Guid.CreateVersion7().ToString("N")[..8].ToUpperInvariant()}",
            Size = request.Size,
            Color = request.Color,
            PriceOverride = request.PriceOverride,
            Stock = request.Stock
        };
        variants.Add(variant);
        await uow.SaveChangesAsync(ct);
        return new VariantDto(variant.Id, variant.Sku, variant.Size, variant.Color,
            variant.PriceOverride ?? product.BasePrice, variant.Stock);
    }

    public async Task UpdateVariantAsync(Guid variantId, SaveVariantRequest request, CancellationToken ct = default)
    {
        var variant = await variants.GetByIdAsync(variantId, ct) ?? throw new NotFoundException("Variant not found.");
        variant.Size = request.Size;
        variant.Color = request.Color;
        variant.PriceOverride = request.PriceOverride;
        variant.Stock = request.Stock;
        await uow.SaveChangesAsync(ct);
    }

    public async Task<ImageDto> AddImageAsync(Guid productId, Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var product = await productQueries.GetByIdWithDetailsAsync(productId, ct)
            ?? throw new NotFoundException("Product not found.");

        var url = await imageStorage.SaveAsync(content, fileName, contentType, ct);
        var image = new ProductImage
        {
            ProductId = productId,
            Url = url,
            AltText = product.Name,
            SortOrder = product.Images.Count + 1,
            IsPrimary = product.Images.Count == 0
        };
        images.Add(image);
        await uow.SaveChangesAsync(ct);
        return new ImageDto(image.Id, image.Url, image.AltText, image.IsPrimary);
    }

    public async Task RemoveImageAsync(Guid imageId, CancellationToken ct = default)
    {
        var image = await images.GetByIdAsync(imageId, ct) ?? throw new NotFoundException("Image not found.");
        images.Remove(image);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<CategoryDto> SaveCategoryAsync(Guid? id, SaveCategoryRequest request, CancellationToken ct = default)
    {
        Category category;
        if (id is { } existingId)
        {
            category = await categories.GetByIdAsync(existingId, ct) ?? throw new NotFoundException("Category not found.");
            category.Name = request.Name.Trim();
            category.ParentId = request.ParentId;
        }
        else
        {
            category = new Category { Name = request.Name.Trim(), Slug = Slugify(request.Name), ParentId = request.ParentId };
            categories.Add(category);
        }
        await uow.SaveChangesAsync(ct);
        return new CategoryDto(category.Id, category.Name, category.Slug, []);
    }

    public async Task<BrandDto> SaveBrandAsync(Guid? id, SaveBrandRequest request, CancellationToken ct = default)
    {
        Brand brand;
        if (id is { } existingId)
        {
            brand = await brands.GetByIdAsync(existingId, ct) ?? throw new NotFoundException("Brand not found.");
            brand.Name = request.Name.Trim();
            brand.LogoUrl = request.LogoUrl;
        }
        else
        {
            brand = new Brand { Name = request.Name.Trim(), Slug = Slugify(request.Name), LogoUrl = request.LogoUrl };
            brands.Add(brand);
        }
        await uow.SaveChangesAsync(ct);
        return new BrandDto(brand.Id, brand.Name, brand.Slug);
    }

    private async Task<string> UniqueSlugAsync(string name, CancellationToken ct)
    {
        var slug = Slugify(name);
        var candidate = slug;
        var i = 1;
        while (await products.AnyAsync(p => p.Slug == candidate, ct))
            candidate = $"{slug}-{++i}";
        return candidate;
    }

    private static string Slugify(string value) => string.Join("-",
        new string(value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
