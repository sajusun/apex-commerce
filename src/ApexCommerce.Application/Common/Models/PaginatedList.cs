namespace ApexCommerce.Application.Common.Models;

public sealed class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageIndex { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public int PageSize { get; }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PaginatedList(IReadOnlyList<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Items = items;
    }
}

