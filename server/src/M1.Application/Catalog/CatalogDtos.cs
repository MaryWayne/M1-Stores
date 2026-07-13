using M1.Application.Common;
using M1.Domain.Entities;

namespace M1.Application.Catalog;

public record ProductQuery(
    string? Search,
    string? CategorySlug,
    string? BrandSlug,
    decimal? MinPrice,
    decimal? MaxPrice,
    double? MinRating,
    bool? InStock,
    bool? Featured,
    string? Sort, // newest | price-asc | price-desc | rating | popular
    int Page = 1,
    int PageSize = 20) : PagingParams(Page, PageSize);

public record ImageDto(Guid Id, string Url, string AltText, bool IsPrimary);

public record VariantDto(Guid Id, string Sku, string? Size, string? Color, decimal Price, int Stock);

public record ProductListItemDto(
    Guid Id, string Name, string Slug, string Category, string? Brand,
    decimal Price, string Currency, string? ImageUrl,
    double AvgRating, int ReviewCount, bool IsFeatured, bool InStock);

public record ProductDetailDto(
    Guid Id, string Name, string Slug, string Description,
    string Category, string CategorySlug, string? Brand,
    decimal Price, string Currency, double AvgRating, int ReviewCount, bool IsFeatured,
    IReadOnlyList<ImageDto> Images, IReadOnlyList<VariantDto> Variants);

public record CategoryDto(Guid Id, string Name, string Slug, IReadOnlyList<CategoryDto> Children);

public record BrandDto(Guid Id, string Name, string Slug);

public static class CatalogMappings
{
    public static decimal PriceOf(ProductVariant v, Product p) => v.PriceOverride ?? p.BasePrice;

    public static decimal MinPriceOf(Product p) =>
        p.Variants.Count == 0 ? p.BasePrice : p.Variants.Min(v => PriceOf(v, p));

    public static ProductListItemDto ToListItem(Product p) => new(
        p.Id, p.Name, p.Slug, p.Category?.Name ?? "", p.Brand?.Name,
        MinPriceOf(p), p.Currency,
        p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).FirstOrDefault()?.Url,
        p.AvgRating, p.ReviewCount, p.IsFeatured,
        p.Variants.Any(v => v.Stock > 0));

    public static ProductDetailDto ToDetail(Product p) => new(
        p.Id, p.Name, p.Slug, p.Description,
        p.Category?.Name ?? "", p.Category?.Slug ?? "", p.Brand?.Name,
        MinPriceOf(p), p.Currency, p.AvgRating, p.ReviewCount, p.IsFeatured,
        p.Images.OrderBy(i => i.SortOrder).Select(i => new ImageDto(i.Id, i.Url, i.AltText, i.IsPrimary)).ToList(),
        p.Variants.Select(v => new VariantDto(v.Id, v.Sku, v.Size, v.Color, PriceOf(v, p), v.Stock)).ToList());
}
