using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using WebUITopic5_Team4.Controllers;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Models;
using WebUITopic5_Team4.Models.ViewModels;
using Xunit;

namespace WebUITopic5_Team4.Tests
{
    /// <summary>
    /// Unit Tests cho CheckOutController (Thanh toán & Đặt hàng)
    /// Xoay quanh các AC nghiệp vụ từ AC 9.1 -> AC 9.6
    /// </summary>
    public class CheckOutControllerTests : IDisposable
    {
        private readonly ElectronicShopContext _context;
        private readonly CheckOutController _controller;
        private const string TestAccountId = "ACC-TEST-002";
        private const string TestCartId = "CART-TEST-002";

        public CheckOutControllerTests()
        {
            var options = new DbContextOptionsBuilder<ElectronicShopContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ElectronicShopContext(options);
            _controller = new CheckOutController(_context);

            // Mock TempData
            var httpContext = new DefaultHttpContext();
            _controller.TempData = new TempDataDictionary(httpContext, new Moq.Mock<ITempDataProvider>().Object);

            // Mock ClaimsPrincipal (User đăng nhập)
            var claims = new List<Claim>
            {
                new Claim("AccountId", TestAccountId),
                new Claim(ClaimTypes.Name, "CheckoutUser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            SeedTestData();
        }

        private void SeedTestData()
        {
            // Seed Account
            _context.Accounts.Add(new Account
            {
                AccountId = TestAccountId,
                Password = "cust_password",
                FullName = "Nguyễn Văn A",
                Email = "nva@gmail.com",
                PhoneNumber = "0987654321",
                IsActive = true,
                RoleId = 1
            });

            // Seed Categories
            _context.Categories.AddRange(
                new Category { CategoryId = 1, CategoryName = "Laptop" },
                new Category { CategoryId = 2, CategoryName = "Điện thoại" }
            );

            // Seed Products
            _context.Products.AddRange(
                new Product
                {
                    ProductId = 21,
                    ProductName = "iPhone 15 Pro Max 256GB",
                    UnitPrice = 29990000,
                    StockQuantity = 5,
                    CategoryId = 2,
                    ImageUrl = "~/images/Products/Phones/iphone15_ProMax_256gb.jpg"
                },
                new Product
                {
                    ProductId = 1,
                    ProductName = "MacBook Air M2 8GB/256GB",
                    UnitPrice = 24990000,
                    StockQuantity = 1,
                    CategoryId = 1,
                    ImageUrl = "~/images/Products/Laptop/Macbook_Air_M2_256GB.jpg"
                }
            );

            // Seed Cart cho User
            _context.Carts.Add(new Cart
            {
                CartId = TestCartId,
                AccountId = TestAccountId
            });

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ============================================================
        // TC_ORD_03: Truy cập trang Checkout khi giỏ hàng trống
        // Kịch bản: Giỏ hàng trống hoàn toàn, truy cập Checkout/Index
        // Kết quả mong đợi: Redirect về Cart/Index với TempData error
        // ============================================================
        [Fact]
        public void TC_ORD_03_Checkout_EmptyCart_RedirectsToCart()
        {
            // Arrange: Đảm bảo giỏ hàng trống (không add sản phẩm nào vào CartItems)

            // Act: Gọi GET: Checkout/Index
            var result = _controller.Index();

            // Assert: Trả về Redirect sang Cart
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Cart", redirectResult.ControllerName);
            Assert.Equal("Giỏ hàng đang trống!", _controller.TempData["error"]);
        }

        // ============================================================
        // TC_CHK_02 & TC_CHK_04: Submit form khi để trống trường bắt buộc hoặc sai định dạng
        // Kịch bản: Giỏ hàng có SP, nhưng Submit form với ModelState không hợp lệ
        // Kết quả mong đợi: Trả về View "Index" cùng ViewModel để hiển thị lỗi
        // ============================================================
        [Fact]
        public void TC_CHK_02_PlaceOrder_InvalidModelState_ReturnsIndexView()
        {
            // Arrange: Thêm sản phẩm vào giỏ hàng
            _context.CartItems.Add(new CartItem
            {
                CartId = TestCartId,
                ProductId = 21,
                Quantity = 1
            });
            _context.SaveChanges();

            // Giả lập ModelState invalid (ví dụ thiếu trường bắt buộc hoặc sai định dạng)
            _controller.ModelState.AddModelError("Checkout.FullName", "Họ và tên là bắt buộc");

            var vm = new CheckoutPageViewModel
            {
                Checkout = new CheckOutViewModel
                {
                    FullName = "", // Bỏ trống
                    Phone = "0987654321",
                    Email = "nva@gmail.com",
                    Address = "TP. Hồ Chí Minh",
                    Province = "Hồ Chí Minh",
                    Note = "Giao giờ hành chính",
                    PaymentMethod = "COD"
                }
            };

            // Act: Gọi POST: PlaceOrder
            var result = _controller.PlaceOrder(vm);

            // Assert: Trả về chính View "Index" với dữ liệu cũ để báo lỗi
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.NotNull(viewResult.Model);
        }

        // ============================================================
        // TC_CHK_06 & TC_ORD_02: Đặt hàng thành công với luồng thông tin hợp lệ
        //                      + Xác thực lại tổng tiền ở Back-end chống sửa HTML
        // Kịch bản: Nhập đúng form, nhấn Place Order.
        // Kết quả mong đợi: Tạo Order thành công, trừ tồn kho, xóa giỏ hàng,
        //                   tổng tiền phải tự tính lại ở Backend và khớp giá thực tế,
        //                   redirect sang Success Page.
        // ============================================================
        [Fact]
        public void TC_CHK_06_And_TC_ORD_02_PlaceOrder_ValidFlow_CreatesOrder_DeductsStock_ClearsCart()
        {
            // Arrange: Thêm sản phẩm ID 21 (iPhone 15 Pro Max 256GB - Tồn kho: 5, Giá: 29.990.000) với số lượng: 2 vào giỏ
            _context.CartItems.Add(new CartItem
            {
                CartId = TestCartId,
                ProductId = 21,
                Quantity = 2
            });
            _context.SaveChanges();

            var vm = new CheckoutPageViewModel
            {
                Checkout = new CheckOutViewModel
                {
                    FullName = "Nguyễn Văn A",
                    Phone = "0987654321",
                    Email = "nva@gmail.com",
                    Address = "123 Đường ABC",
                    Province = "Hồ Chí Minh",
                    Note = "Giao hàng giờ hành chính",
                    PaymentMethod = "COD"
                }
            };

            // Act: Gọi POST: PlaceOrder
            var result = _controller.PlaceOrder(vm);

            // Assert - 1: Phải redirect sang trang Success
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Success", redirectResult.ActionName);
            Assert.NotNull(redirectResult.RouteValues?["id"]);

            int newOrderId = (int)redirectResult.RouteValues["id"]!;

            // Assert - 2: Kiểm tra Đơn hàng (Order) được tạo trong DB
            var createdOrder = _context.Orders.FirstOrDefault(o => o.OrderId == newOrderId);
            Assert.NotNull(createdOrder);
            Assert.Equal("Nguyễn Văn A", createdOrder.FullName);
            Assert.Equal("COD", createdOrder.PaymentMethod);

            // Assert - 3: Kiểm tra tổng tiền tự động tính lại chống sửa HTML
            // 29.990.000 x 2 = 59.980.000đ
            decimal expectedTotal = 29990000m * 2;
            Assert.Equal(expectedTotal, createdOrder.TotalAmount);

            // Assert - 4: Kiểm tra OrderDetail được tạo chính xác
            var orderDetails = _context.OrderDetails.Where(od => od.OrderId == newOrderId).ToList();
            Assert.Single(orderDetails);
            Assert.Equal(21, orderDetails[0].ProductId);
            Assert.Equal(2, orderDetails[0].Quantity);
            Assert.Equal(29990000m, orderDetails[0].UnitPrice);

            // Assert - 5: Kiểm tra tồn kho của sản phẩm bị trừ chính xác (5 - 2 = 3)
            var product = _context.Products.Find(21);
            Assert.Equal(3, product!.StockQuantity);

            // Assert - 6: Kiểm tra giỏ hàng được xóa trống trơn
            var remainingCartItems = _context.CartItems.Where(ci => ci.CartId == TestCartId).ToList();
            Assert.Empty(remainingCartItems);
        }

        // ============================================================
        // TC_ORD_04: Xung đột Session khi đặt hàng (Giỏ hàng trống đột xuất lúc đặt hàng)
        // Kịch bản: Màn hình Checkout đang mở nhưng giỏ hàng đã bị xóa trống trước khi Place Order
        // Kết quả mong đợi: Trả về Redirect sang Cart/Index với TempData error
        // ============================================================
        [Fact]
        public void TC_ORD_04_PlaceOrder_EmptyCartAtExecution_RedirectsToCart()
        {
            // Arrange: Đảm bảo giỏ hàng trống (không có sản phẩm nào)
            var vm = new CheckoutPageViewModel
            {
                Checkout = new CheckOutViewModel
                {
                    FullName = "Nguyễn Văn A",
                    Phone = "0987654321",
                    Email = "nva@gmail.com",
                    Address = "123 Đường ABC",
                    Province = "Hồ Chí Minh",
                    Note = "Giao hàng",
                    PaymentMethod = "COD"
                }
            };

            // Act: Gọi POST: PlaceOrder
            var result = _controller.PlaceOrder(vm);

            // Assert: Hệ thống phát hiện giỏ hàng trống và redirect về Cart kèm thông báo lỗi
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Cart", redirectResult.ControllerName);
            Assert.Equal("Giỏ hàng đang trống!", _controller.TempData["error"]);
        }

        // ============================================================
        // TC_CHK_04: Xác thực định dạng Email và Số điện thoại ở Backend
        // Kịch bản: Nhập SĐT chứa chữ hoặc Email thiếu ký tự @
        // Kết quả mong đợi: ModelState invalid, trả về View "Index" với ViewModel báo lỗi
        // ============================================================
        [Fact]
        public void TC_CHK_04_PlaceOrder_InvalidEmailAndPhone_ReturnsIndexView()
        {
            // Arrange: Thêm sản phẩm vào giỏ hàng
            _context.CartItems.Add(new CartItem
            {
                CartId = TestCartId,
                ProductId = 21,
                Quantity = 1
            });
            _context.SaveChanges();

            // Giả lập lỗi ModelState cho Email và Phone
            _controller.ModelState.AddModelError("Checkout.Email", "Địa chỉ email không hợp lệ");
            _controller.ModelState.AddModelError("Checkout.Phone", "Số điện thoại không hợp lệ (phải là số từ 10 đến 11 ký tự)");

            var vm = new CheckoutPageViewModel
            {
                Checkout = new CheckOutViewModel
                {
                    FullName = "Nguyễn Văn A",
                    Phone = "098765abc", // Sai định dạng SĐT
                    Email = "nva_gmail.com", // Sai định dạng Email
                    Address = "TP. Hồ Chí Minh",
                    Province = "Hồ Chí Minh",
                    Note = "Giao hàng",
                    PaymentMethod = "COD"
                }
            };

            // Act: Gọi POST: PlaceOrder
            var result = _controller.PlaceOrder(vm);

            // Assert: Trả về chính View "Index" để hiển thị lỗi validation
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.NotNull(viewResult.Model);
        }

        // ============================================================
        // TC_E2E_02: E2E Đặt hàng thất bại do hết tồn kho đột xuất (Tranh chấp tồn kho ngầm)
        // Kịch bản: Khách hàng ở bước checkout, sản phẩm trong giỏ có số lượng là 2.
        //           Nhưng ngay trước khi nhấn Đặt hàng, một khách khác mua sạch khiến tồn kho chỉ còn 1.
        // Kết quả mong đợi: Hệ thống chặn tạo đơn hàng, redirect về Cart/Index và thông báo lỗi.
        // ============================================================
        [Fact]
        public void TC_E2E_02_PlaceOrder_InsufficientStockAtExecution_RedirectsToCart()
        {
            // Arrange: Thêm iPhone 15 Pro Max (ID: 21, Tồn kho ban đầu = 5) với số lượng = 2 vào giỏ
            _context.CartItems.Add(new CartItem
            {
                CartId = TestCartId,
                ProductId = 21,
                Quantity = 2
            });
            _context.SaveChanges();

            // Giả lập: Một khách hàng khác mua mất hàng đột ngột, hạ tồn kho thực tế xuống 1
            var product = _context.Products.Find(21);
            Assert.NotNull(product);
            product.StockQuantity = 1; // Chỉ còn 1, không đủ đáp ứng số lượng 2 trong giỏ
            _context.SaveChanges();

            var vm = new CheckoutPageViewModel
            {
                Checkout = new CheckOutViewModel
                {
                    FullName = "Nguyễn Văn A",
                    Phone = "0987654321",
                    Email = "nva@gmail.com",
                    Address = "123 Đường ABC",
                    Province = "Hồ Chí Minh",
                    Note = "Giao hàng",
                    PaymentMethod = "COD"
                }
            };

            // Act: Gọi POST: PlaceOrder
            var result = _controller.PlaceOrder(vm);

            // Assert - 1: Phải redirect sang Cart/Index
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Cart", redirectResult.ControllerName);

            // Assert - 2: TempData phải chứa thông báo lỗi hết hàng tiếng Việt chính xác
            Assert.Equal("Sản phẩm iPhone 15 Pro Max 256GB không đủ hàng!", _controller.TempData["error"]);

            // Assert - 3: Đảm bảo không có Order hay OrderDetail nào được tạo
            var orders = _context.Orders.ToList();
            Assert.Empty(orders);

            // Assert - 4: Tồn kho thực tế vẫn giữ nguyên là 1 (không bị trừ âm)
            var finalProduct = _context.Products.Find(21);
            Assert.Equal(1, finalProduct!.StockQuantity);
        }
    }
}
