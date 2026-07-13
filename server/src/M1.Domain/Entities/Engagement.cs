using M1.Domain.Common;

namespace M1.Domain.Entities;

public class Review : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsVerifiedPurchase { get; set; }
}

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public NotificationType Type { get; set; }
    public required string Title { get; set; }
    public string Body { get; set; } = "";
    public bool IsRead { get; set; }
}
