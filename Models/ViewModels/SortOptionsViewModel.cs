namespace WebUITopic5_Team4.Models.ViewModels
{
    public class SortOptionsViewModel
    {
        public string? SelectedSort { get; set; }

        public int TotalCount { get; set; }

        /// <summary>
        /// Shared query parameters for preserving filters when changing sort.
        /// </summary>
        public ProductIndexQueryViewModel Query { get; set; } = new();

        public IReadOnlyList<(string Value, string Text)> Options { get; set; } =
        [
            ("default", "Default"),
            ("price_asc", "Price: Low to High"),
            ("price_desc", "Price: High to Low"),
            ("newest", "Newest"),
            ("top_rated", "Top Rated"),
            ("discount_desc", "Discount Highest"),
            ("viewcount_desc", "Most Viewed")
        ];
    }
}
