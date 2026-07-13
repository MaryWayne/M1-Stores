using M1.Domain;
using M1.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace M1.Infrastructure.Persistence;

/// <summary>
/// Idempotent startup seeder: creates the admin account, taxonomy, demo catalog
/// and coupons on an empty database so the live demo looks like a real store.
/// </summary>
public static class DbSeeder
{
    private const string Cdn = "https://cdn.dummyjson.com/product-images";

    public static async Task SeedAsync(AppDbContext db, IConfiguration config, ILogger logger)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync())
        {
            logger.LogInformation("Database already seeded — skipping");
            return;
        }

        var hasher = new PasswordHasher<User>();

        var admin = new User
        {
            Email = config["Seed:AdminEmail"] ?? "admin@m1stores.com",
            FullName = "M1 Stores Admin",
            Role = UserRole.Admin,
            EmailVerifiedAt = DateTimeOffset.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, config["Seed:AdminPassword"] ?? "Admin!2026");

        var demo = new User
        {
            Email = "demo@m1stores.com",
            FullName = "Demo Customer",
            EmailVerifiedAt = DateTimeOffset.UtcNow
        };
        demo.PasswordHash = hasher.HashPassword(demo, "Demo!2026");

        db.Users.AddRange(admin, demo);

        var shoes = new Category { Name = "Shoes", Slug = "shoes" };
        var bags = new Category { Name = "Handbags", Slug = "handbags" };
        var cosmetics = new Category { Name = "Cosmetics", Slug = "cosmetics" };
        var jewelry = new Category { Name = "Jewelry", Slug = "jewelry" };
        var accessories = new Category { Name = "Accessories", Slug = "accessories" };
        db.Categories.AddRange(shoes, bags, cosmetics, jewelry, accessories);

        var ck = new Brand { Name = "Calvin Klein", Slug = "calvin-klein" };
        var prada = new Brand { Name = "Prada", Slug = "prada" };
        var heshe = new Brand { Name = "Heshe", Slug = "heshe" };
        var essence = new Brand { Name = "Essence", Slug = "essence" };
        var m1 = new Brand { Name = "M1 Collection", Slug = "m1-collection" };
        db.Brands.AddRange(ck, prada, heshe, essence, m1);

        var shoeSizes = new[] { "37", "38", "39", "40" };
        var oneSize = new[] { "One Size" };

        List<Product> products =
        [
            P("Black & Brown Slipper", 2600, shoes, m1, "The Black & Brown Slipper is a comfortable and stylish choice for casual wear. Soft-soled and easy to slip on for everyday errands.",
                $"{Cdn}/womens-shoes/black-&-brown-slipper", 4, shoeSizes, featured: false),
            P("Calvin Klein Heel Shoes", 10400, shoes, ck, "Calvin Klein Heel Shoes are elegant and sophisticated, designed for formal occasions. A timeless silhouette that pairs with anything.",
                $"{Cdn}/womens-shoes/calvin-klein-heel-shoes", 4, shoeSizes, featured: true),
            P("Golden Shoes Woman", 6500, shoes, m1, "The Golden Shoes for Women are a glamorous choice for special occasions. Metallic finish that catches the light beautifully.",
                $"{Cdn}/womens-shoes/golden-shoes-woman", 4, shoeSizes, featured: true),
            P("Pampi Shoes", 3900, shoes, m1, "Pampi Shoes offer a blend of comfort and style for everyday use. Lightweight, breathable and built to last.",
                $"{Cdn}/womens-shoes/pampi-shoes", 4, shoeSizes, featured: false),
            P("Red Shoes", 4550, shoes, m1, "The Red Shoes make a bold statement with their vibrant red colour. Perfect for adding a pop of confidence to any outfit.",
                $"{Cdn}/womens-shoes/red-shoes", 4, shoeSizes, featured: false),

            P("Blue Women's Handbag", 6500, bags, m1, "The Blue Women's Handbag is a stylish and spacious accessory for everyday use. Roomy interior with organised pockets.",
                $"{Cdn}/womens-bags/blue-women's-handbag", 3, oneSize, featured: false),
            P("Heshe Leather Bag", 16900, bags, heshe, "The Heshe Women's Leather Bag is a luxurious, high-quality leather bag for the sophisticated woman. Genuine leather with premium stitching.",
                $"{Cdn}/womens-bags/heshe-women's-leather-bag", 3, oneSize, featured: true),
            P("Prada Women Bag", 77900, bags, prada, "The Prada Women Bag is an iconic designer bag that exudes elegance and luxury. An investment piece that never goes out of style.",
                $"{Cdn}/womens-bags/prada-women-bag", 3, oneSize, featured: true),
            P("White Faux Leather Backpack", 5200, bags, m1, "The White Faux Leather Backpack is a trendy and practical backpack for the modern woman. Hands-free style for busy days.",
                $"{Cdn}/womens-bags/white-faux-leather-backpack", 3, oneSize, featured: false),
            P("Women Handbag Black", 7800, bags, m1, "The Women Handbag in Black is a classic, versatile accessory that complements every outfit. Structured shape with a premium feel.",
                $"{Cdn}/womens-bags/women-handbag-black", 3, oneSize, featured: false),

            P("Essence Mascara Lash Princess", 1300, cosmetics, essence, "The Essence Mascara Lash Princess is loved for its volumising and lengthening effect. Smudge-proof, dramatic lashes all day.",
                $"{Cdn}/beauty/essence-mascara-lash-princess", 1, oneSize, featured: false),
            P("Eyeshadow Palette with Mirror", 2600, cosmetics, m1, "The Eyeshadow Palette with Mirror offers a versatile range of shades for stunning eye looks. Built-in mirror for touch-ups on the go.",
                $"{Cdn}/beauty/eyeshadow-palette-with-mirror", 1, oneSize, featured: false),
            P("Powder Canister", 1950, cosmetics, m1, "The Powder Canister is a finely milled setting powder designed to set makeup and control shine. Weightless, natural matte finish.",
                $"{Cdn}/beauty/powder-canister", 1, oneSize, featured: false),
            P("Red Lipstick", 1690, cosmetics, m1, "The Red Lipstick is a classic, bold choice for adding a pop of colour to your lips. Long-wearing and richly pigmented.",
                $"{Cdn}/beauty/red-lipstick", 1, oneSize, featured: true),
            P("Red Nail Polish", 1170, cosmetics, m1, "The Red Nail Polish offers a rich, glossy red hue for vibrant, polished nails. Chip-resistant salon finish at home.",
                $"{Cdn}/beauty/red-nail-polish", 1, oneSize, featured: false),

            P("Green Crystal Earring", 3900, jewelry, m1, "The Green Crystal Earring is a dazzling accessory featuring a vibrant green crystal. Adds sparkle to both day and evening looks.",
                $"{Cdn}/womens-jewellery/green-crystal-earring", 3, oneSize, featured: true),
            P("Green Oval Earring", 3250, jewelry, m1, "The Green Oval Earring is a stylish, versatile accessory with a unique oval shape. Lightweight comfort for all-day wear.",
                $"{Cdn}/womens-jewellery/green-oval-earring", 3, oneSize, featured: false),
            P("Tropical Earring", 2600, jewelry, m1, "The Tropical Earring is a fun, playful accessory inspired by tropical elements. Brings holiday energy to any outfit.",
                $"{Cdn}/womens-jewellery/tropical-earring", 3, oneSize, featured: false),

            P("Black Sun Glasses", 3900, accessories, m1, "The Black Sun Glasses are a classic, stylish choice with a sleek black frame and tinted lenses. Full UV protection.",
                $"{Cdn}/sunglasses/black-sun-glasses", 3, oneSize, featured: true),
            P("Classic Sun Glasses", 3250, accessories, m1, "The Classic Sun Glasses offer a timeless design with a neutral frame and UV-protected lenses. Everyday essential.",
                $"{Cdn}/sunglasses/classic-sun-glasses", 3, oneSize, featured: false),
            P("Green and Black Glasses", 4550, accessories, m1, "The Green and Black Glasses feature a bold colour combination that adds vibrancy to your eyewear collection.",
                $"{Cdn}/sunglasses/green-and-black-glasses", 3, oneSize, featured: false),
            P("Party Glasses", 2600, accessories, m1, "The Party Glasses are designed to add flair to your party outfit with unique shapes and colourful frames.",
                $"{Cdn}/sunglasses/party-glasses", 3, oneSize, featured: false),
            P("Everyday Sunglasses", 3000, accessories, m1, "These Sunglasses offer a classic, simple design focused on functionality and essential UV protection.",
                $"{Cdn}/sunglasses/sunglasses", 3, oneSize, featured: false),
        ];

        db.Products.AddRange(products);

        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", Type = CouponType.Percent, Value = 10, MaxUses = 1000, MinOrderTotal = 1000 },
            new Coupon { Code = "M1SAVE500", Type = CouponType.Fixed, Value = 500, MaxUses = 500, MinOrderTotal = 5000 });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} products, 5 categories, 5 brands, 2 coupons, admin + demo users", products.Count);
    }

    private static int _skuCounter = 1000;

    private static Product P(string name, decimal priceKes, Category category, Brand brand,
        string description, string imageBase, int imageCount, string[] sizes, bool featured)
    {
        var slug = name.ToLowerInvariant()
            .Replace("'", "").Replace("&", "and").Replace("  ", " ").Replace(' ', '-');

        var product = new Product
        {
            Name = name,
            Slug = slug,
            Description = description,
            Category = category,
            Brand = brand,
            BasePrice = priceKes,
            IsFeatured = featured
        };

        product.Images = Enumerable.Range(1, imageCount).Select(i => new ProductImage
        {
            Product = product,
            Url = $"{imageBase}/{i}.webp",
            AltText = $"{name} — photo {i}",
            SortOrder = i,
            IsPrimary = i == 1
        }).ToList();

        product.Variants = sizes.Select(size => new ProductVariant
        {
            Product = product,
            Sku = $"M1-{++_skuCounter}",
            Size = size == "One Size" ? null : size,
            Stock = 12 + (_skuCounter % 20)
        }).ToList();

        return product;
    }
}
