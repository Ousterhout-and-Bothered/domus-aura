namespace SmartHome.Domain.Common;

/// <summary>
/// A page of results returned from a paged query, along with the metadata
/// a caller needs to render pagination UI.
/// </summary>
/// <typeparam name="T">The type of item in the page.</typeparam>
/// <param name="Items">The items in this page, in the order produced by the query.</param>
/// <param name="Total">
/// The total number of items matching the query across all pages.
/// </param>
/// <param name="Page">The 1-indexed page number this result represents.</param>
/// <param name="PageSize">The number of items requested per page.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize);