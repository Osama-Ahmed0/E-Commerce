namespace ECommerce.Common
{
    public static class PaginationHelper
    {
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100;

        public static (int PageNumber, int PageSize) Normalize(
            int? pageNumber,
            int? pageSize,
            int defaultPageSize = DefaultPageSize,
            int maxPageSize = MaxPageSize)
        {
            int validPageNumber = pageNumber.HasValue && pageNumber.Value > 0
                ? pageNumber.Value
                : 1;

            int validPageSize = pageSize.HasValue && pageSize.Value > 0
                ? Math.Min(pageSize.Value, maxPageSize)
                : defaultPageSize;

            return (validPageNumber, validPageSize);
        }
    }
}
