using M1.Application.Common;

namespace M1.Tests.Application;

public class PagingTests
{
    [Fact]
    public void PagingParams_ClampsInvalidValues()
    {
        var paging = new PagingParams(Page: -3, PageSize: 5000);
        Assert.Equal(1, paging.SafePage);
        Assert.Equal(100, paging.SafePageSize);
        Assert.Equal(0, paging.Skip);
    }

    [Fact]
    public void PagedResult_ComputesTotalPages()
    {
        var page = new PagedResult<int>([1, 2, 3], Page: 1, PageSize: 10, TotalCount: 23);
        Assert.Equal(3, page.TotalPages);
    }
}
