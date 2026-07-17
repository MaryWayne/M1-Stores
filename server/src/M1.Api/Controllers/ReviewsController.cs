using M1.Application.Common;
using M1.Application.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace M1.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class ReviewsController(ReviewService reviews) : ApiControllerBase
{
    [HttpGet("products/{productId:guid}/reviews")]
    public async Task<ActionResult<PagedResult<ReviewDto>>> List(Guid productId, [FromQuery] PagingParams paging, CancellationToken ct) =>
        Ok(await reviews.ListAsync(productId, paging, ct));

    [Authorize]
    [HttpPost("products/{productId:guid}/reviews")]
    public async Task<ActionResult<ReviewDto>> Create(Guid productId, CreateReviewRequest request, CancellationToken ct) =>
        Ok(await reviews.CreateAsync(CurrentUserId, productId, request, ct));

    [Authorize]
    [HttpDelete("reviews/{reviewId:guid}")]
    public async Task<IActionResult> Delete(Guid reviewId, CancellationToken ct)
    {
        await reviews.DeleteAsync(CurrentUserId, reviewId, User.IsInRole("Admin"), ct);
        return NoContent();
    }
}
