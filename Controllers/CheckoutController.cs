using Microsoft.AspNetCore.Mvc;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Models;
using WebUITopic5_Team4.Models.ViewModels;
using WebUITopic5_Team4.Helpers;

namespace WebUITopic5_Team4.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ElectronicShopContext _context;

        public CheckoutController(ElectronicShopContext context)
        {
            _context = context;
        }

        // ================================
        // 1️⃣ HIỂN THỊ TRANG CHECKOUT
        // ================================
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<Cart>("Cart");

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            return View(cart);
        }

        // ================================
        // 2️⃣ ĐẶT HÀNG (POST)
        // ================================
        [HttpPost]
        public IActionResult PlaceOrder(CheckOutViewModel model)
        {
            var cart = HttpContext.Session.GetObjectFromJson<Cart>("Cart");

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            if (!ModelState.IsValid)
                return RedirectToAction("Index");

            // 👉 tính tổng tiền
            decimal total = cart.CartItems.Sum(x => x.Product.UnitPrice * x.Quantity);

            // ================================
            // 3️⃣ TẠO ORDER
            // ================================
            var order = new Order
            {
                AccountId = null, // nếu có login thì gán sau
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
            _context.SaveChanges(); // ⚠️ phải save để có OrderId

            // ================================
            // 4️⃣ TẠO ORDER DETAILS
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
            }

            _context.SaveChanges();

            // ================================
            // 5️⃣ XOÁ CART SESSION
            // ================================
            HttpContext.Session.Remove("Cart");

            // ================================
            // 6️⃣ CHUYỂN SANG TRANG SUCCESS
            // ================================
            return RedirectToAction("Success");
        }

        // ================================
        // 7️⃣ TRANG ĐẶT HÀNG THÀNH CÔNG
        // ================================
        public IActionResult Success()
        {
            return View();
        }
    }
}