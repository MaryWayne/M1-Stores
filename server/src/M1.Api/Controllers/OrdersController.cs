using M1.Application.Common;
using M1.Application.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace M1.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class OrdersController(OrdersService orders, AddressService addresses, NotificationService notifications) : ApiControllerBase
{
    [HttpPost("checkout/quote")]
    public async Task<ActionResult<QuoteDto>> Quote(QuoteRequest request, CancellationToken ct) =>
        Ok(await orders.QuoteAsync(CurrentUserId, request, ct));

    [HttpPost("checkout")]
    public async Task<ActionResult<object>> Checkout(CheckoutRequest request, CancellationToken ct)
    {
        var (order, redirectUrl) = await orders.CheckoutAsync(CurrentUserId, request, ct);
        return Ok(new { order, redirectUrl });
    }

    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<OrderListItemDto>>> List([FromQuery] PagingParams paging, CancellationToken ct) =>
        Ok(await orders.ListMineAsync(CurrentUserId, paging, ct));

    [HttpGet("orders/{orderNumber}")]
    public async Task<ActionResult<OrderDto>> Get(string orderNumber, CancellationToken ct) =>
        Ok(await orders.GetMineAsync(CurrentUserId, orderNumber, ct));

    [HttpPost("orders/{orderNumber}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(string orderNumber, CancellationToken ct) =>
        Ok(await orders.CancelAsync(CurrentUserId, orderNumber, ct));

    [HttpGet("orders/{orderNumber}/invoice")]
    public async Task<IActionResult> Invoice(string orderNumber, CancellationToken ct) =>
        Content(await orders.InvoiceHtmlAsync(CurrentUserId, orderNumber, ct), "text/html");

    // ---- Addresses ----

    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> Addresses(CancellationToken ct) =>
        Ok(await addresses.ListAsync(CurrentUserId, ct));

    [HttpPost("addresses")]
    public async Task<ActionResult<AddressDto>> CreateAddress(SaveAddressRequest request, CancellationToken ct) =>
        Ok(await addresses.SaveAsync(CurrentUserId, null, request, ct));

    [HttpPut("addresses/{id:guid}")]
    public async Task<ActionResult<AddressDto>> UpdateAddress(Guid id, SaveAddressRequest request, CancellationToken ct) =>
        Ok(await addresses.SaveAsync(CurrentUserId, id, request, ct));

    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        await addresses.DeleteAsync(CurrentUserId, id, ct);
        return NoContent();
    }

    // ---- Notifications ----

    [HttpGet("notifications")]
    public async Task<ActionResult<PagedResult<NotificationDto>>> Notifications([FromQuery] PagingParams paging, CancellationToken ct) =>
        Ok(await notifications.ListAsync(CurrentUserId, paging, ct));

    [HttpGet("notifications/unread-count")]
    public async Task<ActionResult<object>> UnreadCount(CancellationToken ct) =>
        Ok(new { count = await notifications.UnreadCountAsync(CurrentUserId, ct) });

    [HttpPut("notifications/read")]
    public async Task<IActionResult> MarkRead([FromBody] List<Guid>? ids, CancellationToken ct)
    {
        await notifications.MarkReadAsync(CurrentUserId, ids, ct);
        return NoContent();
    }
}
