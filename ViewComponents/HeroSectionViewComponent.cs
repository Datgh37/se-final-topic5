using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Models.ViewModels;

namespace WebUITopic5_Team4.ViewComponents
{
    public class HeroSectionViewComponent : ViewComponent
    {
        private readonly ElectronicShopContext _context;

        public HeroSectionViewComponent(ElectronicShopContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(bool isHome = false)
        {
            var viewModel = new HeroSectionViewModel
            {
                Categories = await _context.Categories.AsNoTracking().OrderBy(x => x.CategoryId).ToListAsync(),
            };

            ViewData["IsHomePage"] = isHome;
            return View(viewModel);
        }
    }
}
