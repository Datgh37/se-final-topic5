using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Models;
using WebUITopic5_Team4.Models.ViewModels;
using WebUITopic5_Team4.Helpers;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;

namespace WebUITopic5_Team4.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ElectronicShopContext _context;

        public CheckoutController(ElectronicShopContext context)
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

        // ================================
        // 1. HIỂN THỊ TRANG CHECKOUT
        // ================================
        public IActionResult Index()
        {
            var cartId = GetOrCreateCartId();
            var cart = _context.Carts
                .Include(x => x.CartItems)
                .ThenInclude(x => x.Product)
                .FirstOrDefault(x => x.CartId == cartId);

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            return View(cart);
        }

        // ================================
        // 2. ĐẶT HÀNG (POST)
        // ================================
        [HttpPost]
        public IActionResult PlaceOrder(CheckOutViewModel model)
        {
            var cartId = GetOrCreateCartId();
            var cart = _context.Carts
                .Include(x => x.CartItems)
                .ThenInclude(x => x.Product)
                .FirstOrDefault(x => x.CartId == cartId);

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            if (!ModelState.IsValid)
                return View("Index", cart);

            // Kiểm tra tồn kho trước khi đặt hàng (Fix TC_E2E_02)
            foreach (var item in cart.CartItems)
            {
                var product = _context.Products.Find(item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"Sản phẩm '{item.Product.ProductName}' đã hết hàng hoặc không đủ số lượng (Chỉ còn {product?.StockQuantity ?? 0}). Vui lòng cập nhật lại giỏ hàng.");
                    return View("Index", cart);
                }
            }

            // tính tổng tiền
            decimal total = cart.CartItems.Sum(x => x.Product.UnitPrice * x.Quantity);

            // ================================
            // 3. TẠO ORDER
            // ================================
            var order = new Order
            {
                AccountId = User.Identity?.IsAuthenticated == true ? User.FindFirst("AccountId")?.Value : null,
                OrderDate = DateTime.Now,
                FullName = model.FullName,
                PhoneNumber = model.Phone,
                Email = model.Email,
                Address = model.Address,
                TownCity = model.Province,
                OrderNotes = model.Note,
                PaymentMethod = model.PaymentMethod,
                TotalAmount = total,
                StatusId = 1 // 1 = Pending
            };

            _context.Orders.Add(order);
            _context.SaveChanges(); // phải save để có OrderId

            // ================================
            // 4. TẠO ORDER DETAILS VÀ TRỪ TỒN KHO
            // ================================
            foreach (var item in cart.CartItems)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.UnitPrice
                };

                _context.OrderDetails.Add(detail);

                // Trừ số lượng tồn kho theo UC-ORDR-02
                var product = _context.Products.Find(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= item.Quantity;
                }
            }

            // ================================
            // 5. XOÁ GIỎ HÀNG TRONG DB
            // ================================
            _context.CartItems.RemoveRange(cart.CartItems);
            _context.SaveChanges();

            // ================================
            // 6. CHUYỂN SANG TRANG SUCCESS
            // ================================
            return RedirectToAction("Success");
        }

        // ================================
        // 7. TRANG ĐẶT HÀNG THÀNH CÔNG
        // ================================
        public IActionResult Success()
        {
            return View();
        }
    }
}