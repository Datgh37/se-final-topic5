using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
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

        // GET: /Cart/Index
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var accountId = User.FindFirst("AccountId")?.Value;
            var cart = await _context.Carts
                .Include(x => x.CartItems)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.AccountId == accountId);

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
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để mua hàng" });
            }

            // Lấy AccountId từ Identity (xác thực người dùng hiện tại)
            var accountId = User.FindFirst("AccountId")?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản" });
            }

            var cart = await _context.Carts
                .Include(x => x.CartItems)
                .FirstOrDefaultAsync(x => x.AccountId == accountId);

            if (cart == null)
            {
                cart = new Cart
                {
                    CartId = Guid.NewGuid().ToString(),
                    AccountId = accountId
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
            
            var summary = await GetCartSummary(accountId);
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
            if (quantity < 1) return Json(new { success = false, message = "Số lượng không hợp lệ" });

            // Lấy danh tính người dùng hiện tại để đảm bảo quyền sở hữu (Chống lỗi IDOR)
            var accountId = User.FindFirst("AccountId")?.Value;
            if (string.IsNullOrEmpty(accountId)) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            var cartItem = await _context.CartItems
                .Include(x => x.Product)
                .Include(x => x.Cart)
                // Chỉ tìm sản phẩm nếu nó thuộc về AccountId của người đang đăng nhập
                .FirstOrDefaultAsync(x => x.CartItemId == cartItemId && x.Cart.AccountId == accountId);

            if (cartItem == null) return Json(new { success = false });

            if (quantity > cartItem.Product.StockQuantity)
            {
                return Json(new { success = false, message = $"Số lượng vượt quá tồn kho ({cartItem.Product.StockQuantity})" });
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            var summary = await GetCartSummary(cartItem.Cart.AccountId);
            
            return Json(new {
                success = true,
                quantity = cartItem.Quantity,
                subtotal = (cartItem.Product.UnitPrice * quantity).ToString("N0"),
                grandTotal = summary.GrandTotal.ToString("N0"),
                totalItems = summary.TotalItems
            });
        }

        // POST: /Cart/DeleteCartItem (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCartItem(int cartItemId)
        {
            // Bảo mật: Xác thực danh tính từ Cookie/Token thay vì tin tưởng ID từ client
            var accountId = User.FindFirst("AccountId")?.Value;
            if (string.IsNullOrEmpty(accountId)) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            var cartItem = await _context.CartItems
                .Include(x => x.Cart)
                // Verification: Kiểm tra chéo ID món hàng với ID chủ sở hữu
                .FirstOrDefaultAsync(x => x.CartItemId == cartItemId && x.Cart.AccountId == accountId);

            if (cartItem == null) return Json(new { success = false });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            var summary = await GetCartSummary(accountId);

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

        // Helper to get cart stats
        private async Task<(int TotalItems, decimal GrandTotal)> GetCartSummary(string accountId)
        {
            var cart = await _context.Carts
                .Include(x => x.CartItems)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.AccountId == accountId);

            if (cart == null) return (0, 0);

            int totalItems = cart.CartItems.Sum(x => x.Quantity);
            decimal grandTotal = cart.CartItems.Sum(x => (decimal)x.Quantity * x.Product.UnitPrice);

            return (totalItems, grandTotal);
        }
    }
}
