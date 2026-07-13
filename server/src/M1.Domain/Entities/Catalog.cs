using M1.Domain.Common;

namespace M1.Domain.Entities;

public class Category : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];
}

public class Brand : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? LogoUrl { get; set; }
}

public class Product : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string Description { get; set; } = "";
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "KES";
    public bool IsFeatured { get; set; }
    public bool IsDeleted { get; set; }

    // Denormalized rating summary, maintained on review writes so listing
    // pages never aggregate over the Reviews table.
    public double AvgRating { get; set; }
    public int ReviewCount { get; set; }

    public List<ProductVariant> Variants { get; set; } = [];
    public List<ProductImage> Images { get; set; } = [];
}

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public required string Sku { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    /// <summary>Overrides the product's BasePrice when set.</summary>
    public decimal? PriceOverride { get; set; }
    public int Stock { get; set; }

    public string Label => string.Join(" / ", new[] { Size, Color }.Where(v => !string.IsNullOrEmpty(v)));
}

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public required string Url { get; set; }
    public string AltText { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}
