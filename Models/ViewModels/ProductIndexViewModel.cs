using WebUITopic5_Team4.Models;
using WebUITopic5_Team4.Models.ViewModels;

namespace WebUITopic5_Team4.Models.ViewModels
{
    public class ProductIndexViewModel
    {
        public ProductIndexQueryViewModel Query { get; set; } = new();

        public PriceFilterViewModel PriceFilter { get; set; } = new();

        public SortOptionsViewModel SortOptions { get; set; } = new();

        public PaginationViewModel Pagination { get; set; } = new();

        public IReadOnlyList<ProductCardViewModel> Products { get; set; } = [];

        public IReadOnlyList<ProductCardViewModel> SaleOffProducts { get; set; } = [];
    }
}
