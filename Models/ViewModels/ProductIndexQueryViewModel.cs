namespace WebUITopic5_Team4.Models.ViewModels
{
    public class ProductIndexQueryViewModel
    {
        public int? CategoryId { get; set; }

        public int? SeriesId { get; set; }

        public string? Keyword { get; set; }

        public string? Sort { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 12;
    }
}
