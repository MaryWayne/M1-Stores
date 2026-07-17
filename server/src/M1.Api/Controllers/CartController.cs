using M1.Application.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace M1.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class CartController(CartService cart) : ApiControllerBase
{
    [HttpGet("cart")]
    public async Task<ActionResult<CartDto>> Get(CancellationToken ct) =>
        Ok(await cart.GetAsync(CurrentUserId, ct));

    [HttpPost("cart/items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemRequest request, CancellationToken ct) =>
        Ok(await cart.AddItemAsync(CurrentUserId, request, ct));

    [HttpPut("cart/items/{itemId:guid}")]
    public async Task<ActionResult<CartDto>> UpdateItem(Guid itemId, UpdateCartItemRequest request, CancellationToken ct) =>
        Ok(await cart.UpdateItemAsync(CurrentUserId, itemId, request, ct));

    [HttpDelete("cart/items/{itemId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid itemId, CancellationToken ct) =>
        Ok(await cart.RemoveItemAsync(CurrentUserId, itemId, ct));

    [HttpPost("cart/merge")]
    public async Task<ActionResult<CartDto>> Merge(MergeCartRequest request, CancellationToken ct) =>
        Ok(await cart.MergeAsync(CurrentUserId, request, ct));

    [HttpGet("wishlist")]
    public async Task<ActionResult<IReadOnlyList<WishlistItemDto>>> Wishlist(CancellationToken ct) =>
        Ok(await cart.GetWishlistAsync(CurrentUserId, ct));

    [HttpPost("wishlist/{productId:guid}")]
    public async Task<IActionResult> AddToWishlist(Guid productId, CancellationToken ct)
    {
        await cart.AddToWishlistAsync(CurrentUserId, productId, ct);
        return NoContent();
    }

    [HttpDelete("wishlist/{productId:guid}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken ct)
    {
        await cart.RemoveFromWishlistAsync(CurrentUserId, productId, ct);
        return NoContent();
    }
}
