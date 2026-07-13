using M1.Domain.Common;

namespace M1.Tests.Domain;

public class BaseEntityTests
{
    private sealed class TestEntity : BaseEntity;

    [Fact]
    public void NewEntity_GetsNonEmptyVersion7Id()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(7, entity.Id.Version);
    }

    [Fact]
    public void NewEntities_GetUniqueIds()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => new TestEntity().Id).ToHashSet();

        Assert.Equal(100, ids.Count);
    }
}
