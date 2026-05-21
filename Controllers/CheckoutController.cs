using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Models;
using WebUITopic5_Team4.Models.ViewModels;

namespace WebUITopic5_Team4.Controllers
{
    public class CheckOutController : Controller
    {
        private readonly ElectronicShopContext _context;

        public CheckOutController(ElectronicShopContext context)
        {
            _context = context;
        }

        // =====================================
        // HELPER: LẤY HOẶC TẠO CART ID
        // =====================================
        private string GetOrCreateCartId()
        {
            // USER LOGIN
            if (User.Identity?.IsAuthenticated == true)
            {
                var accountId = User.FindFirst("AccountId")?.Value;

                var userCart = _context.Carts
                    .FirstOrDefault(c => c.AccountId == accountId);

                if (userCart != null)
                    return userCart.CartId;

                // Tạo cart mới
                string newCartId = Guid.NewGuid().ToString();

                var cart = new Cart
                {
                    CartId = newCartId,
                    AccountId = accountId
                };

                _context.Carts.Add(cart);
                _context.SaveChanges();

                return newCartId;
            }

            // GUEST
            if (Request.Cookies.TryGetValue("CartId", out string cartId))
            {
                var existingCart = _context.Carts
                    .FirstOrDefault(c => c.CartId == cartId);

                if (existingCart != null)
                    return cartId;
            }

            // TẠO CART GUEST MỚI
            string freshCartId = Guid.NewGuid().ToString();

            var guestCart = new Cart
            {
                CartId = freshCartId,
                AccountId = null
            };

            _context.Carts.Add(guestCart);
            _context.SaveChanges();

            Response.Cookies.Append("CartId", freshCartId, new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true,
                IsEssential = true,
                Path = "/"
            });

            return freshCartId;
        }

        // =====================================
        // GET: CHECKOUT PAGE
        // =====================================
        [HttpGet]
        public IActionResult Index()
        {
            var cartId = GetOrCreateCartId();

            var cart = _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefault(c => c.CartId == cartId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["error"] = "Giỏ hàng đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            var vm = new CheckoutPageViewModel
            {
                Cart = cart,
                Checkout = new CheckOutViewModel()
            };

            return View(vm);
        }

        // =====================================
        // POST: PLACE ORDER
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(CheckoutPageViewModel vm)
        {
            // LOAD CART
            var cartId = GetOrCreateCartId();

            var cart = _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefault(c => c.CartId == cartId);

            // GÁN LẠI CART CHO VIEWMODEL
            vm.Cart = cart;

            // CHECK CART
            if (cart == null || !cart.CartItems.Any())
            {
                TempData["error"] = "Giỏ hàng đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            // VALIDATION FAIL
            if (!ModelState.IsValid)
            {
                return View("Index", vm);
            }

            // CHECK STOCK
            foreach (var item in cart.CartItems)
            {
                var product = _context.Products
                    .FirstOrDefault(p => p.ProductId == item.ProductId);

                if (product == null)
                {
                    TempData["error"] =
                        $"Không tìm thấy sản phẩm {item.Product.ProductName}";

                    return RedirectToAction("Index", "Cart");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    TempData["error"] =
                        $"Sản phẩm {item.Product.ProductName} không đủ hàng!";

                    return RedirectToAction("Index", "Cart");
                }
            }

            // TÍNH TỔNG TIỀN
            decimal total = cart.CartItems.Sum(x =>
                x.Product.UnitPrice * x.Quantity
            );

            // TẠO ORDER
            var order = new Order
            {
                AccountId = User.Identity?.IsAuthenticated == true
                    ? User.FindFirst("AccountId")?.Value
                    : null,

                OrderDate = DateTime.Now,

                FullName = vm.Checkout.FullName,

                PhoneNumber = vm.Checkout.Phone,

                Email = vm.Checkout.Email,

                Address = vm.Checkout.Address,

                TownCity = vm.Checkout.Province,

                OrderNotes = vm.Checkout.Note,

                PaymentMethod = vm.Checkout.PaymentMethod,

                TotalAmount = total,

                // FIX THEO DATABASE CỦA BẠN
                StatusId = 1
            };

            _context.Orders.Add(order);

            _context.SaveChanges();

            // ORDER DETAILS + TRỪ KHO
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

                // TRỪ KHO
                item.Product.StockQuantity -= item.Quantity;
            }

            // XOÁ CART ITEMS
            _context.CartItems.RemoveRange(cart.CartItems);

            _context.SaveChanges();

            // SUCCESS
            return RedirectToAction(
                "Success",
                new { id = order.OrderId }
            );
        }

        // =====================================
        // SUCCESS PAGE
        // =====================================
        [HttpGet]
        public IActionResult Success(int id)
        {
            var order = _context.Orders
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }
    }
}