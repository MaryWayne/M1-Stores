using M1.Domain.Common;

namespace M1.Domain.Entities;

public class User : BaseEntity
{
    public required string Email { get; set; }
    /// <summary>Null for accounts created via Google login only.</summary>
    public string? PasswordHash { get; set; }
    public required string FullName { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public string? GoogleId { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public bool IsDeactivated { get; set; }
    public bool IsDeleted { get; set; }

    public List<Address> Addresses { get; set; } = [];

    public bool EmailVerified => EmailVerifiedAt is not null;
}

public class Address : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string Label { get; set; }
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public required string Line1 { get; set; }
    public required string City { get; set; }
    public required string County { get; set; }
    public bool IsDefault { get; set; }
}

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}

public class EmailToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string TokenHash { get; set; }
    public EmailTokenPurpose Purpose { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public bool IsUsable => UsedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
