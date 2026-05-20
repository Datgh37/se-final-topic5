using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Models.ViewModels;

namespace WebUITopic5_Team4.ViewComponents
{
    public class CartViewComponent : ViewComponent
    {
        private readonly ElectronicShopContext _context;

        public CartViewComponent(ElectronicShopContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new CartPreviewViewModel();

            try
            {
                string cartId = null;

                // 1. If user is logged in, try to fetch their account-associated cart ID
                if (User.Identity?.IsAuthenticated == true)
                {
                    var accountId = UserClaimsPrincipal.FindFirst("AccountId")?.Value;
                    if (!string.IsNullOrEmpty(accountId))
                    {
                        var userCart = await _context.Carts.FirstOrDefaultAsync(c => c.AccountId == accountId);
                        if (userCart != null)
                        {
                            cartId = userCart.CartId;
                        }
                    }
                }

                // 2. If no account cart found, check guest cookie
                if (string.IsNullOrEmpty(cartId))
                {
                    Request.Cookies.TryGetValue("CartId", out cartId);
                }

                // 3. Load cart items if a valid CartId is found
                if (!string.IsNullOrEmpty(cartId))
                {
                    var cart = await _context.Carts
                        .Include(x => x.CartItems)
                            .ThenInclude(ci => ci.Product)
                        .FirstOrDefaultAsync(x => x.CartId == cartId);

                    if (cart != null && cart.CartItems != null)
                    {
                        model.TotalItems = cart.CartItems.Sum(x => x.Quantity);
                        model.GrandTotal = cart.CartItems.Sum(x => x.Quantity * x.Product.UnitPrice);
                        model.Items = cart.CartItems
                            .OrderByDescending(x => x.CartItemId)
                            .Select(ci => new CartPreviewItem
                            {
                                CartItemId = ci.CartItemId,
                                ProductId = ci.ProductId,
                                ProductName = ci.Product?.ProductName ?? "Sản phẩm",
                                ImageUrl = ci.Product?.ImageUrl ?? "~/images/product-default.png",
                                UnitPrice = ci.Product?.UnitPrice ?? 0,
                                Quantity = ci.Quantity,
                                StockQuantity = ci.Product?.StockQuantity ?? 0
                            })
                            .ToList();
                    }
                }
            }
            catch
            {
                // Fallback to empty preview if error occurs
            }

            return View(model);
        }
    }
}
