using M1.Application.Catalog;
using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain.Entities;

namespace M1.Application.Shopping;

public class CartService(
    ICartRepository carts,
    IRepository<CartItem> cartItems,
    IRepository<ProductVariant> variants,
    IRepository<WishlistItem> wishlist,
    IProductRepository products,
    IUnitOfWork uow)
{
    public async Task<CartDto> GetAsync(Guid userId, CancellationToken ct = default) =>
        ToDto(await carts.GetOrCreateAsync(userId, ct));

    public async Task<CartDto> AddItemAsync(Guid userId, AddCartItemRequest request, CancellationToken ct = default)
    {
        if (request.Quantity < 1) throw new DomainRuleException("Quantity must be at least 1.");

        var cart = await carts.GetOrCreateAsync(userId, ct);
        var variant = await variants.GetByIdAsync(request.VariantId, ct)
            ?? throw new NotFoundException("Product variant not found.");
        if (variant.Stock < 1) throw new DomainRuleException("This item is out of stock.");

        var existing = cart.Items.FirstOrDefault(i => i.VariantId == variant.Id);
        if (existing is null)
            cartItems.Add(new CartItem { CartId = cart.Id, VariantId = variant.Id, Quantity = Math.Min(request.Quantity, variant.Stock) });
        else
            existing.Quantity = Math.Min(existing.Quantity + request.Quantity, variant.Stock);

        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await uow.SaveChangesAsync(ct);
        return ToDto(await carts.GetOrCreateAsync(userId, ct));
    }

    public async Task<CartDto> UpdateItemAsync(Guid userId, Guid itemId, UpdateCartItemRequest request, CancellationToken ct = default)
    {
        var cart = await carts.GetOrCreateAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Cart item not found.");

        if (request.Quantity < 1)
        {
            cartItems.Remove(item);
        }
        else
        {
            var stock = item.Variant?.Stock ?? 0;
            item.Quantity = Math.Min(request.Quantity, Math.Max(stock, 1));
        }

        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await uow.SaveChangesAsync(ct);
        return ToDto(await carts.GetOrCreateAsync(userId, ct));
    }

    public async Task<CartDto> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var cart = await carts.GetOrCreateAsync(userId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Cart item not found.");
        cartItems.Remove(item);
        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await uow.SaveChangesAsync(ct);
        return ToDto(await carts.GetOrCreateAsync(userId, ct));
    }

    /// <summary>Merges a guest cart (from localStorage) into the user's server cart on login.</summary>
    public async Task<CartDto> MergeAsync(Guid userId, MergeCartRequest request, CancellationToken ct = default)
    {
        foreach (var item in request.Items.Where(i => i.Quantity > 0))
        {
            try { await AddItemAsync(userId, item, ct); }
            catch (NotFoundException) { /* variant vanished — skip */ }
            catch (DomainRuleException) { /* out of stock — skip */ }
        }
        return ToDto(await carts.GetOrCreateAsync(userId, ct));
    }

    // ---- Wishlist ----

    public async Task<IReadOnlyList<WishlistItemDto>> GetWishlistAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await wishlist.ListAsync(w => w.UserId == userId, ct);
        if (items.Count == 0) return [];

        var detailed = await products.GetByIdsWithDetailsAsync(items.Select(w => w.ProductId), ct);
        return detailed.Select(p => new WishlistItemDto(
            p.Id, p.Name, p.Slug, CatalogMappings.MinPriceOf(p),
            p.Images.OrderByDescending(i => i.IsPrimary).FirstOrDefault()?.Url,
            p.AvgRating, p.Variants.Any(v => v.Stock > 0))).ToList();
    }

    public async Task AddToWishlistAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        if (await wishlist.AnyAsync(w => w.UserId == userId && w.ProductId == productId, ct)) return;
        _ = await products.GetByIdWithDetailsAsync(productId, ct)
            ?? throw new NotFoundException("Product not found.");
        wishlist.Add(new WishlistItem { UserId = userId, ProductId = productId });
        await uow.SaveChangesAsync(ct);
    }

    public async Task RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        var item = await wishlist.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);
        if (item is null) return;
        wishlist.Remove(item);
        await uow.SaveChangesAsync(ct);
    }

    // ---- mapping ----

    internal static CartDto ToDto(Cart cart)
    {
        var items = cart.Items
            .Where(i => i.Variant?.Product is not null)
            .OrderBy(i => i.CreatedAt)
            .Select(i =>
            {
                var variant = i.Variant!;
                var product = variant.Product!;
                var price = variant.PriceOverride ?? product.BasePrice;
                return new CartItemDto(
                    i.Id, variant.Id, product.Id, product.Name, product.Slug,
                    variant.Label, product.Images.OrderByDescending(x => x.IsPrimary).FirstOrDefault()?.Url,
                    price, i.Quantity, price * i.Quantity, variant.Stock);
            })
            .ToList();

        return new CartDto(cart.Id, items, items.Sum(i => i.LineTotal), items.Sum(i => i.Quantity));
    }
}
