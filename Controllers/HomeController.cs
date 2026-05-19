using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Helpers;
using WebUITopic5_Team4.Models;
using WebUITopic5_Team4.Models.ViewModels;

namespace WebUITopic5_Team4.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ElectronicShopContext _context;

        public HomeController(ILogger<HomeController> logger, ElectronicShopContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            
            // Random Products for Featured
            var featuredProducts = await _context.Products
                .AsNoTracking()
                .OrderBy(x => Guid.NewGuid()) 
                .Take(8)
                .ProjectToCard()
                .ToListAsync();

            var viewModel = new HomeViewModel
            {
                FeaturedCategories = categories,
                FeaturedProducts = featuredProducts
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
