using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUITopic5_Team4.Data;

namespace WebUITopic5_Team4.ViewComponents
{
    public class ProductSidebarCategoriesViewComponent : ViewComponent
    {
        private readonly ElectronicShopContext _context;

        public ProductSidebarCategoriesViewComponent(ElectronicShopContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories.AsNoTracking().OrderBy(x => x.CategoryId).ToListAsync();
            return View(categories);
        }
    }
}
