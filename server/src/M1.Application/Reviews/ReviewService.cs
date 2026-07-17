using M1.Application.Common;
using M1.Application.Interfaces;
using M1.Domain;
using M1.Domain.Entities;

namespace M1.Application.Reviews;

public record CreateReviewRequest(int Rating, string Title, string Body);
public record ReviewDto(Guid Id, Guid UserId, string UserName, int Rating, string Title, string Body,
    bool IsVerifiedPurchase, DateTimeOffset CreatedAt);

public class ReviewService(
    IRepository<Review> reviews,
    IRepository<Product> products,
    IRepository<OrderItem> orderItems,
    IRepository<User> users,
    IUnitOfWork uow)
{
    public async Task<PagedResult<ReviewDto>> ListAsync(Guid productId, PagingParams paging, CancellationToken ct = default)
    {
        var all = await reviews.ListAsync(r => r.ProductId == productId, ct);
        var ordered = all.OrderByDescending(r => r.CreatedAt).ToList();
        var userIds = ordered.Select(r => r.UserId).Distinct().ToList();
        var authors = (await users.ListAsync(u => userIds.Contains(u.Id), ct)).ToDictionary(u => u.Id, u => u.FullName);

        var page = ordered.Skip(paging.Skip).Take(paging.SafePageSize)
            .Select(r => new ReviewDto(r.Id, r.UserId, authors.GetValueOrDefault(r.UserId, "Customer"),
                r.Rating, r.Title, r.Body, r.IsVerifiedPurchase, r.CreatedAt))
            .ToList();
        return new PagedResult<ReviewDto>(page, paging.SafePage, paging.SafePageSize, ordered.Count);
    }

    public async Task<ReviewDto> CreateAsync(Guid userId, Guid productId, CreateReviewRequest request, CancellationToken ct = default)
    {
        if (request.Rating is < 1 or > 5)
            throw new DomainRuleException("Rating must be between 1 and 5.");

        var product = await products.GetByIdAsync(productId, ct)
            ?? throw new NotFoundException("Product not found.");

        if (await reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId, ct))
            throw new DomainRuleException("You have already reviewed this product.");

        var hasPurchased = await orderItems.AnyAsync(i =>
            i.Order!.UserId == userId
            && i.Variant!.ProductId == productId
            && i.Order.Status != OrderStatus.Cancelled
            && i.Order.Status != OrderStatus.PendingPayment, ct);

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = request.Rating,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            IsVerifiedPurchase = hasPurchased
        };
        reviews.Add(review);

        await RecalculateAsync(product, pending: review, remove: null, ct);
        await uow.SaveChangesAsync(ct);

        var author = await users.GetByIdAsync(userId, ct);
        return new ReviewDto(review.Id, userId, author?.FullName ?? "Customer",
            review.Rating, review.Title, review.Body, review.IsVerifiedPurchase, review.CreatedAt);
    }

    public async Task DeleteAsync(Guid userId, Guid reviewId, bool isAdmin, CancellationToken ct = default)
    {
        var review = await reviews.GetByIdAsync(reviewId, ct)
            ?? throw new NotFoundException("Review not found.");
        if (review.UserId != userId && !isAdmin)
            throw new UnauthorizedException("You can only delete your own reviews.");

        reviews.Remove(review);
        var product = await products.GetByIdAsync(review.ProductId, ct);
        if (product is not null)
            await RecalculateAsync(product, pending: null, remove: review, ct);
        await uow.SaveChangesAsync(ct);
    }

    /// <summary>Keeps the denormalized AvgRating/ReviewCount on Product in sync.</summary>
    private async Task RecalculateAsync(Product product, Review? pending, Review? remove, CancellationToken ct)
    {
        var existing = await reviews.ListAsync(r => r.ProductId == product.Id, ct);
        var effective = existing.Where(r => remove is null || r.Id != remove.Id).ToList();
        if (pending is not null && effective.All(r => r.Id != pending.Id)) effective.Add(pending);

        product.ReviewCount = effective.Count;
        product.AvgRating = effective.Count == 0 ? 0 : Math.Round(effective.Average(r => r.Rating), 2);
    }
}
