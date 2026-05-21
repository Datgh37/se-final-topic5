using WebUITopic5_Team4.Models;
using WebUITopic5_Team4.Models.ViewModels;

namespace WebUITopic5_Team4.Helpers
{
    public static class ProductQueryExtensions
    {
        public static IQueryable<ProductCardViewModel> ProjectToCard(this IQueryable<Product> query)
        {
            return query.Select(x => new ProductCardViewModel
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                UnitPrice = x.UnitPrice,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.CategoryName,
                PrimaryImage = x.ImageUrl,
                StockQuantity = x.StockQuantity,
            });
        }
    }
}
