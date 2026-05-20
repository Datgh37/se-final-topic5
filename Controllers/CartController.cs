using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Models;

namespace WebUITopic5_Team4.Controllers
{
    public class CartController : Controller
    {
        private readonly ElectronicShopContext _context;

        public CartController(ElectronicShopContext context)
        {
            _context = context;
        }

        // Helper to get or create Cart ID from Cookie or Account
        private string GetOrCreateCartId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var accountId = User.FindFirst("AccountId")?.Value;
                if (!string.IsNullOrEmpty(accountId))
                {
                    // Find cart by AccountId
                    var userCart = _context.Carts.FirstOrDefault(c => c.AccountId == accountId);
                    if (userCart != null)
                    {
                        return userCart.CartId;
                    }

                    // Otherwise, see if there's a guest cart in cookies that we can assign to this account
                    if (Request.Cookies.TryGetValue("CartId", out string guestCartId))
                    {
                        var existingCart = _context.Carts.FirstOrDefault(c => c.CartId == guestCartId);
                        if (existingCart != null && string.IsNullOrEmpty(existingCart.AccountId))
                        {
                            existingCart.AccountId = accountId;
                            _context.SaveChanges();
                            return guestCartId;
                        }
                    }

                    // Create new cart for account
                    string newCartId = Guid.NewGuid().ToString();
                    var newCart = new Cart
                    {
                        CartId = newCartId,
                        AccountId = accountId
                    };
                    _context.Carts.Add(newCart);
                    _context.SaveChanges();
                    return newCartId;
                }
            }

            // Guest flow
            if (Request.Cookies.TryGetValue("CartId", out string cookieCartId))
            {
                var existingCart = _context.Carts.FirstOrDefault(c => c.CartId == cookieCartId);
                if (existingCart != null)
                {
                    return cookieCartId;
                }
            }

            // Create new guest cart
            string freshCartId = Guid.NewGuid().ToString();
            var guestCart = new Cart
            {
                CartId = freshCartId,
                AccountId = null
            };
            _context.Carts.Add(guestCart);
            _context.SaveChanges();

            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true,
                IsEssential = true,
                Path = "/"
            };
            Response.Cookies.Append("CartId", freshCartId, cookieOptions);

            return freshCartId;
        }

        // GET: /Cart/Index
        public async Task<IActionResult> Index()
        {
            var cartId = GetOrCreateCartId();
            var cart = await _context.Carts
                .Include(x => x.CartItems)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.CartId == cartId);

            if (cart == null)
            {
                return View(new List<CartItem>());
            }

            return View(cart.CartItems.ToList());
        }

        // POST: /Cart/AddToCart (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0) return Json(new { success = false, message = "Số lượng không hợp lệ" });

            var cartId = GetOrCreateCartId();
            var cart = await _context.Carts
                .Include(x => x.CartItems)
                .FirstOrDefaultAsync(x => x.CartId == cartId);

            if (cart == null)
            {
                cart = new Cart
                {
                    CartId = cartId,
                    AccountId = User.Identity?.IsAuthenticated == true ? User.FindFirst("AccountId")?.Value : null
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = await _context.CartItems
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.CartId == cart.CartId && x.ProductId == productId);

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại" });

            if (cartItem != null)
            {
                if (cartItem.Quantity + quantity > product.StockQuantity)
                {
                    return Json(new { success = false, message = $"Chỉ còn {product.StockQuantity} sản phẩm trong kho" });
                }
                cartItem.Quantity += quantity;
            }
            else
            {
                if (quantity > product.StockQuantity)
                {
                    return Json(new { success = false, message = $"Chỉ còn {product.StockQuantity} sản phẩm trong kho" });
                }
                cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            
            var summary = await GetCartSummary(cartId);
            return Json(new { 
                success = true, 
                totalItems = summary.TotalItems, 
                message = "Đã thêm vào giỏ hàng" 
            });
        }

        // POST: /Cart/UpdateQuantity (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            // TC_CART_05: Nếu quantity <= 0 hoặc không hợp lệ → reset về 1
            if (quantity < 1)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ, đã reset về 1", resetQty = 1 });
            }

            var cartId = GetOrCreateCartId();
            var cartItem = await _context.CartItems
                .Include(x => x.Product)
                .Include(x => x.Cart)
                .FirstOrDefaultAsync(x => x.CartItemId == cartItemId && x.CartId == cartId);

            if (cartItem == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ" });

            // TC_CART_06: Số lượng > tồn kho → trả về maxStock để JS tự set
            if (quantity > cartItem.Product.StockQuantity)
            {
                return Json(new
                {
                    success = false,
                    message = $"Số lượng vượt quá tồn kho ({cartItem.Product.StockQuantity})",
                    maxStock = cartItem.Product.StockQuantity
                });
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            var summary = await GetCartSummary(cartId);

            return Json(new
            {
                success = true,
                quantity = cartItem.Quantity,
                subtotal = (cartItem.Product.UnitPrice * quantity).ToString("N0"),
                grandTotal = summary.GrandTotal.ToString("N0"),
                totalItems = summary.TotalItems,
                maxStock = cartItem.Product.StockQuantity
            });
        }

        // POST: /Cart/DeleteCartItem (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCartItem(int cartItemId)
        {
            var cartId = GetOrCreateCartId();
            var cartItem = await _context.CartItems
                .Include(x => x.Cart)
                .FirstOrDefaultAsync(x => x.CartItemId == cartItemId && x.CartId == cartId);

            if (cartItem == null) return Json(new { success = false });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            var summary = await GetCartSummary(cartId);

            return Json(new {
                success = true,
                totalItems = summary.TotalItems,
                grandTotal = summary.GrandTotal.ToString("N0")
            });
        }

        // GET: /Cart/GetCartPreview (AJAX - to reload the component)
        public IActionResult GetCartPreview()
        {
            return ViewComponent("Cart");
        }

        // Helper to get cart stats by CartId
        private async Task<(int TotalItems, decimal GrandTotal)> GetCartSummary(string cartId)
        {
            var cart = await _context.Carts
                .Include(x => x.CartItems)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.CartId == cartId);

            if (cart == null) return (0, 0);

            int totalItems = cart.CartItems.Sum(x => x.Quantity);
            decimal grandTotal = cart.CartItems.Sum(x => (decimal)x.Quantity * x.Product.UnitPrice);

            return (totalItems, grandTotal);
        }
    }
}
