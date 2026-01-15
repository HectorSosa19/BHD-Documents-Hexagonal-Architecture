namespace Domain.Repositories;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; }
    public int TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }

    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items ?? Enumerable.Empty<T>();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PagedResult<T>(Enumerable.Empty<T>(), 0, pageNumber, pageSize);
    }
}