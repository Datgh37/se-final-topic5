namespace WebUITopic5_Team4.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public int PageSize { get; set; }

        public bool ShowArrows { get; set; } = true;

        /// <summary>
        /// Shared query parameters for generating page links.
        /// </summary>
        public ProductIndexQueryViewModel Query { get; set; } = new();

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
