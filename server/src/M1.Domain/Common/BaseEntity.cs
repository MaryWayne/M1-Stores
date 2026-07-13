namespace M1.Domain.Common;

/// <summary>
/// Base for all entities. Guid v7 keys: non-guessable in public URLs while
/// remaining time-ordered for healthy database indexes.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
