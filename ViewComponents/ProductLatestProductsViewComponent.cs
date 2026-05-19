using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Helpers;

namespace WebUITopic5_Team4.ViewComponents
{
    public class ProductLatestProductsViewComponent : ViewComponent
    {
        private readonly ElectronicShopContext _context;

        public ProductLatestProductsViewComponent(ElectronicShopContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int take = 6, string type = "latest")
        {
            var query = _context.Products.AsNoTracking();

            switch (type.ToLower())
            {
                case "toprated":
                    query = query.OrderByDescending(x => x.UnitPrice);
                    break;
                case "popular":
                    query = query.OrderByDescending(x => x.StockQuantity);
                    break;
                default:
                    query = query.OrderByDescending(x => x.ProductId);
                    break;
            }

            var products = await query
                .Take(take)
                .ProjectToCard()
                .ToListAsync();

            return View(products);
        }
    }
}
