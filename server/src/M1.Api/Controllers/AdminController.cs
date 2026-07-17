using M1.Application.Admin;
using M1.Application.Common;
using M1.Application.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace M1.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(AdminService admin) : ApiControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct) =>
        Ok(await admin.GetDashboardAsync(ct));

    [HttpGet("reports/sales")]
    public async Task<ActionResult<IReadOnlyList<SalesPointDto>>> Sales(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] string groupBy = "day", CancellationToken ct = default) =>
        Ok(await admin.GetSalesReportAsync(from, to, groupBy, ct));

    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<OrderDto>>> Orders(
        [FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] PagingParams paging, CancellationToken ct = default)
    {
        var page = await admin.ListOrdersAsync(status, search, paging, ct);
        return Ok(new PagedResult<OrderDto>(
            page.Items.Select(OrdersService.ToDto).ToList(), page.Page, page.PageSize, page.TotalCount));
    }

    [HttpPut("orders/{orderId:guid}/status")]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(Guid orderId, UpdateOrderStatusRequest request, CancellationToken ct) =>
        Ok(await admin.UpdateOrderStatusAsync(orderId, request, ct));

    [HttpGet("customers")]
    public async Task<ActionResult<PagedResult<CustomerDto>>> Customers(
        [FromQuery] string? search, [FromQuery] PagingParams paging, CancellationToken ct = default) =>
        Ok(await admin.ListCustomersAsync(search, paging, ct));

    [HttpPut("customers/{userId:guid}/active")]
    public async Task<IActionResult> SetCustomerActive(Guid userId, [FromBody] bool active, CancellationToken ct)
    {
        await admin.SetCustomerActiveAsync(userId, active, ct);
        return NoContent();
    }

    [HttpGet("coupons")]
    public async Task<ActionResult<IReadOnlyList<CouponDto>>> Coupons(CancellationToken ct) =>
        Ok(await admin.ListCouponsAsync(ct));

    [HttpPost("coupons")]
    public async Task<ActionResult<CouponDto>> CreateCoupon(SaveCouponRequest request, CancellationToken ct) =>
        Ok(await admin.SaveCouponAsync(null, request, ct));

    [HttpPut("coupons/{id:guid}")]
    public async Task<ActionResult<CouponDto>> UpdateCoupon(Guid id, SaveCouponRequest request, CancellationToken ct) =>
        Ok(await admin.SaveCouponAsync(id, request, ct));

    [HttpDelete("coupons/{id:guid}")]
    public async Task<IActionResult> DeleteCoupon(Guid id, CancellationToken ct)
    {
        await admin.DeleteCouponAsync(id, ct);
        return NoContent();
    }
}
