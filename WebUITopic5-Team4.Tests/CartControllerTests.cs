using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebUITopic5_Team4.Controllers;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Models;
using Xunit;

namespace WebUITopic5_Team4.Tests
{
    /// <summary>
    /// Unit Tests cho CartController
    /// 
    /// ── Kiểm thử xoay vòng (Cross-Testing) ──
    /// TC_CART_01 → TC_CART_09: Kiểm thử module Giỏ hàng (code của Đạt)
    ///   → Người thực thi: Phạm Gia Bảo (Task 3.1)
    /// TC_E2E_01: Kiểm thử Tích hợp E2E luồng Giỏ hàng → Đặt hàng
    ///   → Người thực thi: Trịnh Thành Đạt - 1923050149 (Task 3.3)
    /// </summary>
    public class CartControllerTests : IDisposable
    {
        private readonly ElectronicShopContext _context;
        private readonly CartController _controller;
        private const string TestAccountId = "ACC-TEST-001";

        public CartControllerTests()
        {
            // Tạo InMemory Database cho mỗi test case (tránh data lẫn nhau)
            var options = new DbContextOptionsBuilder<ElectronicShopContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ElectronicShopContext(options);
            _controller = new CartController(_context);

            // Mock HttpContext với ClaimsPrincipal (giả lập user đã đăng nhập)
            var claims = new List<Claim>
            {
                new Claim("AccountId", TestAccountId),
                new Claim(ClaimTypes.Name, "TestUser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Seed dữ liệu mẫu
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Tạo Account
            _context.Accounts.Add(new Account
            {
                AccountId = TestAccountId,
                Password = "hashed_pass",
                FullName = "Test User",
                Email = "test@test.com",
                PhoneNumber = "0901234567",
                IsActive = true,
                RoleId = 1
            });

            // Tạo Category
            _context.Categories.Add(new Category
            {
                CategoryId = 1,
                CategoryName = "Điện thoại"
            });

            // Tạo Products (dùng cho các test case)
            _context.Products.AddRange(
                new Product
                {
                    ProductId = 1,
                    ProductName = "iPhone 16 Pro Max",
                    UnitPrice = 34990000,
                    StockQuantity = 10,
                    CategoryId = 1,
                    ImageUrl = "~/images/Products/iphone16.jpg"
                },
                new Product
                {
                    ProductId = 2,
                    ProductName = "Samsung Galaxy S25 Ultra",
                    UnitPrice = 29990000,
                    StockQuantity = 5,
                    CategoryId = 1,
                    ImageUrl = "~/images/Products/samsung-s25.jpg"
                },
                new Product
                {
                    ProductId = 3,
                    ProductName = "Laptop Dell XPS 15",
                    UnitPrice = 45000000,
                    StockQuantity = 0, // Hết hàng
                    CategoryId = 1,
                    ImageUrl = "~/images/Products/dell-xps.jpg"
                }
            );

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ============================================================
        // TC_CART_01: Thêm sản phẩm vào giỏ hàng thành công
        // AC 1.1: Nếu nhập số lượng hợp lệ và bấm ADD TO CART,
        //         sản phẩm được thêm vào giỏ và có thông báo thành công
        // ============================================================
        [Fact]
        public async Task TC_CART_01_AddToCart_ValidQuantity_ReturnsSuccess()
        {
            // Act
            var result = await _controller.AddToCart(productId: 1, quantity: 2);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;

            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));
            Assert.Equal("Đã thêm vào giỏ hàng",
                (string)data.GetType().GetProperty("message")!.GetValue(data));

            // Verify: Cart item tồn tại trong DB với quantity = 2
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(x => x.ProductId == 1);
            Assert.NotNull(cartItem);
            Assert.Equal(2, cartItem.Quantity);
        }

        // ============================================================
        // TC_CART_02: Thêm sản phẩm đã tồn tại → cộng dồn
        // AC 1.2: Sản phẩm đã có trong giỏ → cộng dồn số lượng
        // ============================================================
        [Fact]
        public async Task TC_CART_02_AddToCart_ExistingProduct_QuantityAccumulates()
        {
            // Arrange: Thêm lần 1 với quantity = 2
            await _controller.AddToCart(productId: 1, quantity: 2);

            // Act: Thêm lần 2 với quantity = 1
            var result = await _controller.AddToCart(productId: 1, quantity: 1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Verify: Chỉ có 1 CartItem, quantity = 3 (cộng dồn)
            var cartItems = await _context.CartItems.Where(x => x.ProductId == 1).ToListAsync();
            Assert.Single(cartItems);
            Assert.Equal(3, cartItems[0].Quantity);
        }

        // ============================================================
        // TC_CART_03: Thêm sản phẩm vượt tồn kho → chặn
        // AC 1.3: Số lượng > tồn kho → chặn và báo lỗi
        // ============================================================
        [Fact]
        public async Task TC_CART_03_AddToCart_ExceedsStock_ReturnsFail()
        {
            // Act: Thêm 15 sản phẩm (tồn kho chỉ có 10)
            var result = await _controller.AddToCart(productId: 1, quantity: 15);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.False((bool)data.GetType().GetProperty("success")!.GetValue(data));

            var message = (string)data.GetType().GetProperty("message")!.GetValue(data);
            Assert.Contains("10", message); // Phải chứa số tồn kho

            // Verify: Không có CartItem nào được tạo
            var cartItems = await _context.CartItems.ToListAsync();
            Assert.Empty(cartItems);
        }

        // ============================================================
        // TC_CART_04: Cập nhật số lượng hợp lệ
        // AC 2.1: Cập nhật thành công → Total thay đổi
        // ============================================================
        [Fact]
        public async Task TC_CART_04_UpdateQuantity_ValidQuantity_ReturnsNewTotals()
        {
            // Arrange: Tạo cart item
            await _controller.AddToCart(productId: 1, quantity: 1);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 1);

            // Act: Cập nhật số lượng thành 3
            var result = await _controller.UpdateQuantity(cartItem.CartItemId, 3);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));
            Assert.Equal(3, (int)data.GetType().GetProperty("quantity")!.GetValue(data));

            // Verify subtotal = 34,990,000 * 3 = 104,970,000
            var subtotal = (string)data.GetType().GetProperty("subtotal")!.GetValue(data);
            Assert.Equal("104,970,000", subtotal);
        }

        // ============================================================
        // TC_CART_05: Cập nhật số lượng không hợp lệ (0, -1, NaN)
        // AC 2.2: Reset về 1 hoặc báo lỗi
        // ============================================================
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task TC_CART_05_UpdateQuantity_InvalidValues_ReturnsResetQty(int invalidQty)
        {
            // Arrange
            await _controller.AddToCart(productId: 1, quantity: 2);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 1);

            // Act
            var result = await _controller.UpdateQuantity(cartItem.CartItemId, invalidQty);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.False((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Server phải trả resetQty = 1
            Assert.Equal(1, (int)data.GetType().GetProperty("resetQty")!.GetValue(data));
        }

        // ============================================================
        // TC_CART_06: Cập nhật số lượng lớn hơn tồn kho
        // AC 2.3: Báo lỗi và trả về maxStock
        // ============================================================
        [Fact]
        public async Task TC_CART_06_UpdateQuantity_ExceedsStock_ReturnsMaxStock()
        {
            // Arrange
            await _controller.AddToCart(productId: 1, quantity: 1);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 1);

            // Act: Cập nhật thành 15 (tồn kho = 10)
            var result = await _controller.UpdateQuantity(cartItem.CartItemId, 15);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.False((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Server phải trả maxStock = 10
            Assert.Equal(10, (int)data.GetType().GetProperty("maxStock")!.GetValue(data));

            // Verify: Quantity trong DB vẫn giữ nguyên giá trị cũ (1)
            var updatedItem = await _context.CartItems.FirstAsync(x => x.CartItemId == cartItem.CartItemId);
            Assert.Equal(1, updatedItem.Quantity);
        }

        // ============================================================
        // TC_CART_07: Xóa một sản phẩm bất kỳ
        // AC 3.1: SP bị xóa, trừ tiền, giảm badge
        // ============================================================
        [Fact]
        public async Task TC_CART_07_DeleteCartItem_Success_ReducesTotals()
        {
            // Arrange: Thêm 2 sản phẩm khác nhau
            await _controller.AddToCart(productId: 1, quantity: 1);
            await _controller.AddToCart(productId: 2, quantity: 1);
            var cartItems = await _context.CartItems.ToListAsync();
            Assert.Equal(2, cartItems.Count);

            // Act: Xóa sản phẩm thứ 2
            var itemToDelete = cartItems.First(x => x.ProductId == 2);
            var result = await _controller.DeleteCartItem(itemToDelete.CartItemId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Verify: Chỉ còn 1 sản phẩm trong giỏ
            var remaining = await _context.CartItems.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(1, remaining[0].ProductId);
        }

        // ============================================================
        // TC_CART_08: Xóa sản phẩm cuối cùng → giỏ hàng trống
        // AC 3.2: Hiển thị "Giỏ hàng trống", ẩn nút Checkout
        // ============================================================
        [Fact]
        public async Task TC_CART_08_DeleteLastItem_CartBecomesEmpty()
        {
            // Arrange: Chỉ thêm 1 sản phẩm
            await _controller.AddToCart(productId: 1, quantity: 1);
            var cartItem = await _context.CartItems.FirstAsync();

            // Act: Xóa sản phẩm duy nhất
            var result = await _controller.DeleteCartItem(cartItem.CartItemId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));
            Assert.Equal(0, (int)data.GetType().GetProperty("totalItems")!.GetValue(data));

            // Verify: Không còn CartItem nào trong DB
            var items = await _context.CartItems.ToListAsync();
            Assert.Empty(items);
        }

        // ============================================================
        // TC_CART_09: Badge số lượng cập nhật tức thời (kiểm tra server-side)
        // AC 4.1, AC 4.2: totalItems & grandTotal luôn chính xác
        // ============================================================
        [Fact]
        public async Task TC_CART_09_CartSummary_AlwaysAccurate()
        {
            // Arrange: Thêm 2 SP
            await _controller.AddToCart(productId: 1, quantity: 2); // 2 * 34,990,000 = 69,980,000
            await _controller.AddToCart(productId: 2, quantity: 1); // 1 * 29,990,000 = 29,990,000

            // Act: Lấy kết quả từ Index (server returns tổng hợp)
            var indexResult = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(indexResult);
            var model = Assert.IsType<List<CartItem>>(viewResult.Model);
            Assert.Equal(2, model.Count);

            int totalQty = model.Sum(x => x.Quantity);
            Assert.Equal(3, totalQty); // 2 + 1

            decimal totalPrice = model.Sum(x => x.Quantity * x.Product.UnitPrice);
            Assert.Equal(99_970_000m, totalPrice); // 69,980,000 + 29,990,000
        }

        // ============================================================
        // Additional: Kiểm tra khách vãng lai chưa đăng nhập (Guest Flow)
        // ============================================================
        [Fact]
        public async Task AddToCart_UnauthenticatedUser_CreatesGuestCart_ReturnsSuccess()
        {
            // Arrange: Tạo controller không có auth (Guest Flow)
            var guestController = new CartController(_context);
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated
            guestController.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };

            // Act
            var result = await guestController.AddToCart(productId: 1, quantity: 1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Verify: CartItem và Cart vãng lai được tạo trong DB với AccountId = null
            var cartItems = await _context.CartItems.Include(x => x.Cart).ToListAsync();
            var guestItem = cartItems.FirstOrDefault(x => x.ProductId == 1);
            Assert.NotNull(guestItem);
            Assert.Null(guestItem.Cart.AccountId);
            Assert.Equal(1, guestItem.Quantity);
        }

        // ============================================================
        // Additional: Thêm sản phẩm không tồn tại
        // ============================================================
        [Fact]
        public async Task AddToCart_NonExistentProduct_ReturnsFail()
        {
            // Act
            var result = await _controller.AddToCart(productId: 999, quantity: 1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.False((bool)data.GetType().GetProperty("success")!.GetValue(data));

            var message = (string)data.GetType().GetProperty("message")!.GetValue(data);
            Assert.Contains("không tồn tại", message);
        }

        // ============================================================
        // Additional: Xóa CartItem không thuộc về user hiện tại (IDOR test)
        // ============================================================
        [Fact]
        public async Task DeleteCartItem_OtherUsersItem_ReturnsFail()
        {
            // Arrange: Tạo cart item cho user khác
            var otherCart = new Cart { CartId = "OTHER-CART", AccountId = "OTHER-ACC" };
            _context.Carts.Add(otherCart);
            _context.CartItems.Add(new CartItem
            {
                CartId = "OTHER-CART",
                ProductId = 1,
                Quantity = 1
            });
            await _context.SaveChangesAsync();

            var otherItem = await _context.CartItems.FirstAsync(x => x.CartId == "OTHER-CART");

            // Act: User hiện tại cố xóa item của user khác
            var result = await _controller.DeleteCartItem(otherItem.CartItemId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.False((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Verify: Item vẫn tồn tại (không bị xóa)
            var stillExists = await _context.CartItems.AnyAsync(x => x.CartItemId == otherItem.CartItemId);
            Assert.True(stillExists);
        }

        // ============================================================
        // TC_CART_10: Tự động đồng bộ số lượng khi bấm Thanh toán (Safe Checkout)
        // AC liên quan: Bổ sung (Safe Checkout)
        // Kịch bản: Khách tăng số lượng của sản phẩm nhưng không nhấn nút cập nhật, 
        //           hệ thống tự động kích hoạt đồng bộ UpdateQuantity thành công lên server.
        // ============================================================
        [Fact]
        public async Task TC_CART_10_SafeCheckout_UpdatesQuantitySuccessfully()
        {
            // Arrange: Tạo cart item ban đầu với quantity = 1
            await _controller.AddToCart(productId: 1, quantity: 1);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 1);
            Assert.Equal(1, cartItem.Quantity);

            // Act: Khách thay đổi số lượng thành 2 và trigger Safe Checkout (gọi UpdateQuantity lên Server)
            var result = await _controller.UpdateQuantity(cartItem.CartItemId, 2);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));
            Assert.Equal(2, (int)data.GetType().GetProperty("quantity")!.GetValue(data));

            // Verify: Database lưu số lượng mới là 2
            var updatedItem = await _context.CartItems.FirstAsync(x => x.CartItemId == cartItem.CartItemId);
            Assert.Equal(2, updatedItem.Quantity);
        }

        // ============================================================
        // TC_E2E_01: Kiểm thử Tích hợp (E2E) luồng đặt hàng toàn diện
        // Người thực thi: Trịnh Thành Đạt (Task 3.3)
        // Kịch bản: Thêm 2 SP → Cập nhật SL SP1 lên 2 → Verify tổng
        //           → Verify tồn kho không bị trừ trước khi đặt hàng
        // AC liên quan: AC 1.1 → AC 9.6 (toàn bộ luồng)
        // ============================================================
        [Fact]
        public async Task TC_E2E_01_FullCartFlow_AddUpdateVerifyTotals()
        {
            // ─── B1: Khách thêm 2 sản phẩm khác nhau vào giỏ ───
            var addResult1 = await _controller.AddToCart(productId: 1, quantity: 1);
            var addJson1 = Assert.IsType<JsonResult>(addResult1);
            Assert.True((bool)addJson1.Value!.GetType().GetProperty("success")!.GetValue(addJson1.Value));

            var addResult2 = await _controller.AddToCart(productId: 2, quantity: 1);
            var addJson2 = Assert.IsType<JsonResult>(addResult2);
            Assert.True((bool)addJson2.Value!.GetType().GetProperty("success")!.GetValue(addJson2.Value));

            // Verify: 2 CartItems tồn tại
            var cartItemsAfterAdd = await _context.CartItems.Include(x => x.Product).ToListAsync();
            Assert.Equal(2, cartItemsAfterAdd.Count);

            // ─── B2: Vào giỏ hàng, cập nhật số lượng SP1 lên 2 ───
            var item1 = cartItemsAfterAdd.First(x => x.ProductId == 1);
            var updateResult = await _controller.UpdateQuantity(item1.CartItemId, 2);
            var updateJson = Assert.IsType<JsonResult>(updateResult);
            dynamic updateData = updateJson.Value!;
            Assert.True((bool)updateData.GetType().GetProperty("success")!.GetValue(updateData));
            Assert.Equal(2, (int)updateData.GetType().GetProperty("quantity")!.GetValue(updateData));

            // ─── B5: Kiểm tra tổng tiền và số lượng chính xác ───
            // SP1: iPhone 16 Pro Max → 34,990,000 × 2 = 69,980,000
            // SP2: Samsung Galaxy S25 Ultra → 29,990,000 × 1 = 29,990,000
            // Grand Total = 99,970,000
            var indexResult = await _controller.Index();
            var viewResult = Assert.IsType<ViewResult>(indexResult);
            var model = Assert.IsType<List<CartItem>>(viewResult.Model);

            Assert.Equal(2, model.Count);

            int totalQty = model.Sum(x => x.Quantity);
            Assert.Equal(3, totalQty); // 2 + 1

            decimal grandTotal = model.Sum(x => x.Quantity * x.Product.UnitPrice);
            Assert.Equal(99_970_000m, grandTotal);

            // ─── Verify: Tồn kho chưa bị trừ (chưa đặt hàng) ───
            var product1 = await _context.Products.FindAsync(1);
            var product2 = await _context.Products.FindAsync(2);
            Assert.Equal(10, product1!.StockQuantity); // Vẫn nguyên 10
            Assert.Equal(5, product2!.StockQuantity);  // Vẫn nguyên 5

            // ─── Verify: Subtotal từng dòng chính xác ───
            var sp1InCart = model.First(x => x.ProductId == 1);
            var sp2InCart = model.First(x => x.ProductId == 2);
            Assert.Equal(69_980_000m, sp1InCart.Quantity * sp1InCart.Product.UnitPrice);
            Assert.Equal(29_990_000m, sp2InCart.Quantity * sp2InCart.Product.UnitPrice);
        }

        // ============================================================
        // TC_E2E_02: E2E Đặt hàng thất bại do hết tồn kho đột xuất (Tranh chấp tồn kho ngầm)
        // Kịch bản: Khách thêm SP MacBook Air M2 (tồn kho ban đầu = 1) vào giỏ.
        //           Trước khi thanh toán/đặt hàng, giả lập tồn kho SP đó bị đổi về 0.
        //           Khi đặt hàng hoặc cập nhật giỏ hàng sẽ bị chặn.
        // ============================================================
        [Fact]
        public async Task TC_E2E_02_StockDepletion_ReturnsFail()
        {
            // Arrange: Sản phẩm 2 (Samsung S25 Ultra) có tồn kho = 5.
            await _controller.AddToCart(productId: 2, quantity: 5);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 2);
            Assert.Equal(5, cartItem.Quantity);

            // Giả lập tồn kho của sản phẩm 2 bị thay đổi về 0 (do khách khác mua mất)
            var product = await _context.Products.FindAsync(2);
            product!.StockQuantity = 0;
            await _context.SaveChangesAsync();

            // Act: Khách hiện tại cố gắng cập nhật giỏ hàng hoặc thực hiện hành động liên quan tới sản phẩm đó
            var result = await _controller.UpdateQuantity(cartItem.CartItemId, 5);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.False((bool)data.GetType().GetProperty("success")!.GetValue(data));
            Assert.Equal(0, (int)data.GetType().GetProperty("maxStock")!.GetValue(data));
            Assert.Contains("vượt quá tồn kho", (string)data.GetType().GetProperty("message")!.GetValue(data));
        }

        // ============================================================
        // TC_E2E_03: E2E Thao tác giỏ hàng phức tạp và Thanh toán an toàn ngầm (Safe Checkout)
        // Kịch bản: Thêm 3 sản phẩm (ID 1, ID 2, ID 4) -> Xóa sản phẩm ID 4 ->
        //           Tăng sản phẩm ID 2 từ 1 lên 2 (không bấm nút cập nhật giỏ hàng) ->
        //           Đồng bộ ngầm (gọi UpdateQuantity và Delete) ->
        //           Kiểm tra Database thấy giỏ hàng chỉ còn ID 1 (1 cái) và ID 2 (2 cái).
        // ============================================================
        [Fact]
        public async Task TC_E2E_03_ComplexCartOperations_SafeCheckout_Success()
        {
            // Seed thêm sản phẩm ID 4
            _context.Products.Add(new Product
            {
                ProductId = 4,
                ProductName = "MacBook Air M3",
                UnitPrice = 32990000,
                StockQuantity = 5,
                CategoryId = 1,
                ImageUrl = "~/images/Products/macbook-air.jpg"
            });
            await _context.SaveChangesAsync();

            // B1: Thêm 3 sản phẩm vào giỏ
            await _controller.AddToCart(productId: 1, quantity: 1);
            await _controller.AddToCart(productId: 2, quantity: 1);
            await _controller.AddToCart(productId: 4, quantity: 1);

            var cartItems = await _context.CartItems.ToListAsync();
            Assert.Equal(3, cartItems.Count);

            var itemA = cartItems.First(x => x.ProductId == 1);
            var itemB = cartItems.First(x => x.ProductId == 2);
            var itemC = cartItems.First(x => x.ProductId == 4);

            // B2: Nhấn "X" để xóa sản phẩm C (ID 4)
            var deleteResult = await _controller.DeleteCartItem(itemC.CartItemId);
            var deleteJson = Assert.IsType<JsonResult>(deleteResult);
            Assert.True((bool)deleteJson.Value!.GetType().GetProperty("success")!.GetValue(deleteJson.Value));

            // B3: Bấm "+" để tăng sản phẩm B (ID 2) từ 1 lên 2 (đồng bộ Safe Checkout ngầm)
            var updateResult = await _controller.UpdateQuantity(itemB.CartItemId, 2);
            var updateJson = Assert.IsType<JsonResult>(updateResult);
            Assert.True((bool)updateJson.Value!.GetType().GetProperty("success")!.GetValue(updateJson.Value));

            // B4 & B5: Kiểm tra Database sau khi đồng bộ
            var finalCartItems = await _context.CartItems.Include(x => x.Product).ToListAsync();
            Assert.Equal(2, finalCartItems.Count); // Chỉ còn 2 sản phẩm A và B

            var remainingA = finalCartItems.First(x => x.ProductId == 1);
            var remainingB = finalCartItems.First(x => x.ProductId == 2);

            Assert.Equal(1, remainingA.Quantity);
            Assert.Equal(2, remainingB.Quantity);

            // Tổng giá trị = 1 * 34,990,000 + 2 * 29,990,000 = 94,970,000
            decimal expectedTotal = 1 * 34990000m + 2 * 29990000m;
            decimal actualTotal = finalCartItems.Sum(x => x.Quantity * x.Product.UnitPrice);
            Assert.Equal(expectedTotal, actualTotal);
        }
    }
}
