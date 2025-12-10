namespace Application.Domain.Model.Dtos
{
    public class PagedResult<T>
    {
        public required IReadOnlyCollection<T> Items { get; init; }
        public int Total { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
    }
}
