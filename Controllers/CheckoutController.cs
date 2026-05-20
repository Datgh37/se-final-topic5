using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUITopic5_Team4.Data;


namespace WebUITopic5_Team4.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ElectronicShopContext _context;

        public CheckoutController(ElectronicShopContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var accountId = User.FindFirst("AccountId")?.Value;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.AccountId == accountId);

            if (cart == null)
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cart);
        }
    }
}
