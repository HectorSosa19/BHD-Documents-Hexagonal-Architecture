using System.ComponentModel.DataAnnotations;

namespace Aplication.DTOs.Requests.Pagination;

public class PaginationRequest
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;
    private const int DefaultPageNumber = 1;

    private int _pageNumber = DefaultPageNumber;
    private int _pageSize = DefaultPageSize;

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? DefaultPageNumber : value;
    }

    [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? DefaultPageSize : value);
    }

    public int Skip => (PageNumber - 1) * PageSize;
}