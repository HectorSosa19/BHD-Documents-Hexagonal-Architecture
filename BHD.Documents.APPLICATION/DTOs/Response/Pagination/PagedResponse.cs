namespace Aplication.DTOs.Response.Pagination;

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public int FirstItemOnPage => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int LastItemOnPage => Math.Min(PageNumber * PageSize, TotalCount);
    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedResponse<T> Create(IEnumerable<T> source, int pageNumber, int pageSize, int totalCount)
    {
        return new PagedResponse<T>(source, pageNumber, pageSize, totalCount);
    }
}