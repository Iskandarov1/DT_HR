namespace DT_HR.Contract.Common;

/// <summary>
/// Represents the generic paged list.
/// </summary>
/// <typeparam name="T">The type of list.</typeparam>
public sealed class PagedList<T>
{
	public PagedList(IEnumerable<T> items, int page, int pageSize, int totalCount)
	{
		Page = page;
		PageSize = pageSize;
		TotalCount = totalCount;
		Items = items.ToList();
	}

	/// <summary>
	/// Gets the current page.
	/// </summary>
	public int Page { get; }

	/// <summary>
	/// Gets the page size. The maximum page size is 100.
	/// </summary>
	//[JsonPropertyName("page_size")]
	public int PageSize { get; }

	/// <summary>
	/// Gets the total number of items.
	/// </summary>
	//[JsonPropertyName("total_count")]
	public int TotalCount { get; }

	/// <summary>
	/// Gets the flag indicating whether the next page exists.
	/// </summary>
	//[JsonPropertyName("has_next_page")]
	public bool HasNextPage => (Page < 1 ? 1 : Page) * PageSize < TotalCount;

	/// <summary>
	/// Gets the flag indicating whether the previous page exists.
	/// </summary>
	//[JsonPropertyName("has_previous_page")]
	public bool HasPreviousPage => Page > 1;

	/// <summary>
	/// Gets the items.
	/// </summary>
	//[JsonPropertyName("items")]
	public IReadOnlyCollection<T> Items { get; }
}
