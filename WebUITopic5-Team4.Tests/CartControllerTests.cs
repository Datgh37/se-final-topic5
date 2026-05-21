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

            // Tạo Categories
            _context.Categories.AddRange(
                new Category { CategoryId = 1, CategoryName = "Laptop" },
                new Category { CategoryId = 2, CategoryName = "Điện thoại" }
            );

            // Tạo Products (Khớp 100% với Schema_data.sql và CSV Test Cases)
            _context.Products.AddRange(
                new Product
                {
                    ProductId = 1,
                    ProductName = "MacBook Air M2 8GB/256GB",
                    UnitPrice = 24990000,
                    StockQuantity = 1, // Tồn kho thực tế = 1
                    CategoryId = 1,
                    ImageUrl = "~/images/Products/Laptop/Macbook_Air_M2_256GB.jpg"
                },
                new Product
                {
                    ProductId = 2,
                    ProductName = "MacBook Pro M3 16GB/512GB",
                    UnitPrice = 45990000,
                    StockQuantity = 5,
                    CategoryId = 1,
                    ImageUrl = "~/images/Products/Laptop/Macbook_Pro_M3_512GB.jpg"
                },
                new Product
                {
                    ProductId = 3,
                    ProductName = "MacBook Air M3 16GB/512GB",
                    UnitPrice = 32990000,
                    StockQuantity = 5,
                    CategoryId = 1,
                    ImageUrl = "~/images/Products/Laptop/Macbook_Air_M3_512GB.jpg"
                },
                new Product
                {
                    ProductId = 6,
                    ProductName = "Dell Vostro 3430",
                    UnitPrice = 12490000,
                    StockQuantity = 0, // Hết hàng
                    CategoryId = 1,
                    ImageUrl = "~/images/Products/Laptop/Dell_Vostro_3430.jpg"
                },
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
                    ProductId = 22,
                    ProductName = "iPhone 15 128GB",
                    UnitPrice = 19990000,
                    StockQuantity = 15,
                    CategoryId = 2,
                    ImageUrl = "~/images/Products/Phones/iphone15_128GB.jpg"
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
        // AC 1.1: Thêm sản phẩm hợp lệ, hiển thị thông báo thành công
        // Dữ liệu test: iPhone 15 Pro Max 256GB (ID: 21), Số lượng: 1
        // ============================================================
        [Fact]
        public async Task TC_CART_01_AddToCart_ValidQuantity_ReturnsSuccess()
        {
            // Act: Thêm sản phẩm iPhone 15 Pro Max 256GB (ID: 21) với số lượng = 1
            var result = await _controller.AddToCart(productId: 21, quantity: 1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;

            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));
            Assert.Equal("Đã thêm vào giỏ hàng",
                (string)data.GetType().GetProperty("message")!.GetValue(data));

            // Verify: Cart item tồn tại trong DB với quantity = 1
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(x => x.ProductId == 21);
            Assert.NotNull(cartItem);
            Assert.Equal(1, cartItem.Quantity);
        }

        // ============================================================
        // TC_CART_02: Thêm sản phẩm đã tồn tại → cộng dồn
        // AC 1.2: Sản phẩm đã có trong giỏ → cộng dồn số lượng
        // Dữ liệu test: iPhone 15 Pro Max 256GB (ID: 21), Số lượng: 1 + 1 = 2
        // ============================================================
        [Fact]
        public async Task TC_CART_02_AddToCart_ExistingProduct_QuantityAccumulates()
        {
            // Arrange: Thêm lần 1 với quantity = 1
            await _controller.AddToCart(productId: 21, quantity: 1);

            // Act: Thêm lần 2 với quantity = 1
            var result = await _controller.AddToCart(productId: 21, quantity: 1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Verify: Chỉ có 1 CartItem, quantity = 2 (cộng dồn)
            var cartItems = await _context.CartItems.Where(x => x.ProductId == 21).ToListAsync();
            Assert.Single(cartItems);
            Assert.Equal(2, cartItems[0].Quantity);
        }

        // ============================================================
        // TC_CART_03: Thêm sản phẩm vượt quá số lượng tồn kho hiện có
        // AC 1.3: Thêm quá stock -> Lần 1 thành công, lần 2 bị chặn
        // Dữ liệu test: MacBook Air M2 8GB/256GB (ID: 1 - tồn kho thực tế = 1), số lượng thêm: 1+1 = 2
        // ============================================================
        [Fact]
        public async Task TC_CART_03_AddToCart_ExceedsStock_ReturnsFail()
        {
            // Act - Lần 1: Thêm 1 sản phẩm ID 1 (tồn kho = 1) -> Thành công
            var result1 = await _controller.AddToCart(productId: 1, quantity: 1);
            var jsonResult1 = Assert.IsType<JsonResult>(result1);
            dynamic data1 = jsonResult1.Value!;
            Assert.True((bool)data1.GetType().GetProperty("success")!.GetValue(data1));

            // Act - Lần 2: Thêm tiếp 1 sản phẩm ID 1 nữa -> Bị chặn
            var result2 = await _controller.AddToCart(productId: 1, quantity: 1);

            // Assert
            var jsonResult2 = Assert.IsType<JsonResult>(result2);
            dynamic data2 = jsonResult2.Value!;
            Assert.False((bool)data2.GetType().GetProperty("success")!.GetValue(data2));

            var message = (string)data2.GetType().GetProperty("message")!.GetValue(data2);
            Assert.Equal("Chỉ còn 1 sản phẩm trong kho", message);

            // Verify: Số lượng trong DB vẫn chỉ là 1
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(x => x.ProductId == 1);
            Assert.NotNull(cartItem);
            Assert.Equal(1, cartItem.Quantity);
        }

        // ============================================================
        // TC_CART_04: Cập nhật số lượng sản phẩm hợp lệ trong giỏ hàng
        // AC 2.1: Cập nhật thành công → Total thay đổi
        // Dữ liệu test: iPhone 15 Pro Max 256GB (ID: 21), Đơn giá: 29.990.000đ, Cập nhật lên số lượng: 3
        // ============================================================
        [Fact]
        public async Task TC_CART_04_UpdateQuantity_ValidQuantity_ReturnsNewTotals()
        {
            // Arrange: Tạo cart item
            await _controller.AddToCart(productId: 21, quantity: 1);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 21);

            // Act: Cập nhật số lượng thành 3
            var result = await _controller.UpdateQuantity(cartItem.CartItemId, 3);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));
            Assert.Equal(3, (int)data.GetType().GetProperty("quantity")!.GetValue(data));

            // Verify subtotal = 29,990,000 * 3 = 89,970,000
            var subtotal = (string)data.GetType().GetProperty("subtotal")!.GetValue(data);
            Assert.Equal("89,970,000", subtotal);
        }

        // ============================================================
        // TC_CART_05: Cập nhật số lượng sản phẩm không hợp lệ (số âm, 0, chữ)
        // AC 2.2: Reset về 1 hoặc báo lỗi
        // Dữ liệu test: iPhone 15 Pro Max 256GB (ID: 21), cập nhật về 0, -1
        // ============================================================
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task TC_CART_05_UpdateQuantity_InvalidValues_ReturnsResetQty(int invalidQty)
        {
            // Arrange
            await _controller.AddToCart(productId: 21, quantity: 2);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 21);

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
        // TC_CART_06: Cập nhật số lượng lớn hơn mức tồn kho
        // AC 2.3: Báo lỗi và trả về maxStock
        // Dữ liệu test: MacBook Pro M3 16GB/512GB (ID: 2 - tồn kho thực tế = 5), Cập nhật lên: 8
        // ============================================================
        [Fact]
        public async Task TC_CART_06_UpdateQuantity_ExceedsStock_ReturnsMaxStock()
        {
            // Arrange
            await _controller.AddToCart(productId: 2, quantity: 1);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 2);

            // Act: Cập nhật thành 8 (tồn kho = 5)
            var result = await _controller.UpdateQuantity(cartItem.CartItemId, 8);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.False((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Server phải trả maxStock = 5
            Assert.Equal(5, (int)data.GetType().GetProperty("maxStock")!.GetValue(data));

            // Verify: Quantity trong DB vẫn giữ nguyên giá trị cũ (1)
            var updatedItem = await _context.CartItems.FirstAsync(x => x.CartItemId == cartItem.CartItemId);
            Assert.Equal(1, updatedItem.Quantity);
        }

        // ============================================================
        // TC_CART_07: Xóa một sản phẩm bất kỳ khỏi giỏ hàng
        // AC 3.1: SP bị xóa, trừ tiền, giảm badge
        // Dữ liệu test: Thêm iPhone 15 Pro Max (ID: 21) và iPhone 15 (ID: 22), Xóa ID 22
        // ============================================================
        [Fact]
        public async Task TC_CART_07_DeleteCartItem_Success_ReducesTotals()
        {
            // Arrange: Thêm 2 sản phẩm khác nhau
            await _controller.AddToCart(productId: 21, quantity: 1);
            await _controller.AddToCart(productId: 22, quantity: 1);
            var cartItems = await _context.CartItems.ToListAsync();
            Assert.Equal(2, cartItems.Count);

            // Act: Xóa sản phẩm iPhone 15 128GB (ID: 22)
            var itemToDelete = cartItems.First(x => x.ProductId == 22);
            var result = await _controller.DeleteCartItem(itemToDelete.CartItemId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Verify: Chỉ còn 1 sản phẩm trong giỏ (ID: 21)
            var remaining = await _context.CartItems.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(21, remaining[0].ProductId);
        }

        // ============================================================
        // TC_CART_08: Xóa sản phẩm cuối cùng khiến giỏ hàng trống
        // AC 3.2: Hiển thị "Giỏ hàng trống", ẩn nút PROCEED TO CHECKOUT
        // ============================================================
        [Fact]
        public async Task TC_CART_08_DeleteLastItem_CartBecomesEmpty()
        {
            // Arrange: Chỉ thêm 1 sản phẩm
            await _controller.AddToCart(productId: 21, quantity: 1);
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
        // TC_CART_09: Kiểm tra badge số lượng và tổng tiền cập nhật tức thời
        // AC 4.1, AC 4.2: totalItems & grandTotal luôn chính xác
        // Dữ liệu test: iPhone 15 Pro Max (ID: 21) SL: 2, iPhone 15 (ID: 22) SL: 1
        // ============================================================
        [Fact]
        public async Task TC_CART_09_CartSummary_AlwaysAccurate()
        {
            // Arrange: Thêm 2 SP
            await _controller.AddToCart(productId: 21, quantity: 2); // 2 * 29,990,000 = 59,980,000
            await _controller.AddToCart(productId: 22, quantity: 1); // 1 * 19,990,000 = 19,990,000

            // Act: Lấy kết quả từ Index (server returns tổng hợp)
            var indexResult = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(indexResult);
            var model = Assert.IsType<List<CartItem>>(viewResult.Model);
            Assert.Equal(2, model.Count);

            int totalQty = model.Sum(x => x.Quantity);
            Assert.Equal(3, totalQty); // 2 + 1

            decimal totalPrice = model.Sum(x => x.Quantity * x.Product.UnitPrice);
            Assert.Equal(79970000m, totalPrice); // 59,980,000 + 19,990,000
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
            var result = await guestController.AddToCart(productId: 21, quantity: 1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data));

            // Verify: CartItem và Cart vãng lai được tạo trong DB với AccountId = null
            var cartItems = await _context.CartItems.Include(x => x.Cart).ToListAsync();
            var guestItem = cartItems.FirstOrDefault(x => x.ProductId == 21);
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
                ProductId = 21,
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
        // Dữ liệu test: iPhone 15 Pro Max 256GB (ID: 21), tăng số lượng lên 2
        // ============================================================
        [Fact]
        public async Task TC_CART_10_SafeCheckout_UpdatesQuantitySuccessfully()
        {
            // Arrange: Tạo cart item ban đầu với quantity = 1
            await _controller.AddToCart(productId: 21, quantity: 1);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 21);
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
        // AC liên quan: AC 1.1 → AC 9.6 (toàn bộ luồng)
        // Dữ liệu test: MacBook Pro M3 16GB/512GB (ID: 2), iPhone 15 128GB (ID: 22)
        //               Sửa số lượng SP 1 (ID 2) từ 1 lên 2
        // ============================================================
        [Fact]
        public async Task TC_E2E_01_FullCartFlow_AddUpdateVerifyTotals()
        {
            // ─── B1: Khách thêm 2 sản phẩm khác nhau vào giỏ ───
            var addResult1 = await _controller.AddToCart(productId: 2, quantity: 1);
            var addJson1 = Assert.IsType<JsonResult>(addResult1);
            Assert.True((bool)addJson1.Value!.GetType().GetProperty("success")!.GetValue(addJson1.Value));

            var addResult2 = await _controller.AddToCart(productId: 22, quantity: 1);
            var addJson2 = Assert.IsType<JsonResult>(addResult2);
            Assert.True((bool)addJson2.Value!.GetType().GetProperty("success")!.GetValue(addJson2.Value));

            // Verify: 2 CartItems tồn tại
            var cartItemsAfterAdd = await _context.CartItems.Include(x => x.Product).ToListAsync();
            Assert.Equal(2, cartItemsAfterAdd.Count);

            // ─── B2: Vào giỏ hàng, cập nhật số lượng SP1 lên 2 ───
            var item1 = cartItemsAfterAdd.First(x => x.ProductId == 2);
            var updateResult = await _controller.UpdateQuantity(item1.CartItemId, 2);
            var updateJson = Assert.IsType<JsonResult>(updateResult);
            dynamic updateData = updateJson.Value!;
            Assert.True((bool)updateData.GetType().GetProperty("success")!.GetValue(updateData));
            Assert.Equal(2, (int)updateData.GetType().GetProperty("quantity")!.GetValue(updateData));

            // ─── B5: Kiểm tra tổng tiền và số lượng chính xác ───
            // SP 1: MacBook Pro M3 (ID: 2) -> 45,990,000 × 2 = 91,980,000đ
            // SP 2: iPhone 15 128GB (ID: 22) -> 19,990,000 × 1 = 19,990,000đ
            // Grand Total = 111,970,000đ
            var indexResult = await _controller.Index();
            var viewResult = Assert.IsType<ViewResult>(indexResult);
            var model = Assert.IsType<List<CartItem>>(viewResult.Model);

            Assert.Equal(2, model.Count);

            int totalQty = model.Sum(x => x.Quantity);
            Assert.Equal(3, totalQty); // 2 + 1

            decimal grandTotal = model.Sum(x => x.Quantity * x.Product.UnitPrice);
            Assert.Equal(111970000m, grandTotal);

            // ─── Verify: Tồn kho chưa bị trừ (chưa đặt hàng) ───
            var product1 = await _context.Products.FindAsync(2);
            var product2 = await _context.Products.FindAsync(22);
            Assert.Equal(5, product1!.StockQuantity); // Vẫn nguyên 5
            Assert.Equal(15, product2!.StockQuantity);  // Vẫn nguyên 15

            // ─── Verify: Subtotal từng dòng chính xác ───
            var sp1InCart = model.First(x => x.ProductId == 2);
            var sp2InCart = model.First(x => x.ProductId == 22);
            Assert.Equal(91980000m, sp1InCart.Quantity * sp1InCart.Product.UnitPrice);
            Assert.Equal(19990000m, sp2InCart.Quantity * sp2InCart.Product.UnitPrice);
        }

        // ============================================================
        // TC_E2E_02: E2E Đặt hàng thất bại do hết tồn kho đột xuất (Tranh chấp tồn kho ngầm)
        // Dữ liệu test: MacBook Air M2 8GB/256GB (ID: 1 - tồn kho ban đầu = 1)
        // ============================================================
        [Fact]
        public async Task TC_E2E_02_StockDepletion_ReturnsFail()
        {
            // Arrange: Thêm sản phẩm MacBook Air M2 (tồn kho ban đầu = 1)
            await _controller.AddToCart(productId: 1, quantity: 1);
            var cartItem = await _context.CartItems.FirstAsync(x => x.ProductId == 1);
            Assert.Equal(1, cartItem.Quantity);

            // Giả lập tồn kho của sản phẩm 1 bị thay đổi về 0 (do khách khác mua mất)
            var product = await _context.Products.FindAsync(1);
            product!.StockQuantity = 0;
            await _context.SaveChangesAsync();

            // Act: Khách hiện tại cố gắng cập nhật giỏ hàng hoặc thực hiện hành động liên quan tới sản phẩm đó
            var result = await _controller.UpdateQuantity(cartItem.CartItemId, 1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value!;
            Assert.False((bool)data.GetType().GetProperty("success")!.GetValue(data));
            Assert.Equal(0, (int)data.GetType().GetProperty("maxStock")!.GetValue(data));
            Assert.Contains("vượt quá tồn kho", (string)data.GetType().GetProperty("message")!.GetValue(data));
        }

        // ============================================================
        // TC_E2E_03: E2E Thao tác giỏ hàng phức tạp và Thanh toán an toàn ngầm (Safe Checkout)
        // Dữ liệu test:
        // - Sản phẩm A: MacBook Pro M3 16GB/512GB (ID: 2)
        // - Sản phẩm B: iPhone 15 128GB (ID: 22)
        // - Sản phẩm C: MacBook Air M3 16GB/512GB (ID: 3)
        // Thao tác: Xóa C (ID 3), Tăng B (ID 22) từ 1 lên 2.
        // ============================================================
        [Fact]
        public async Task TC_E2E_03_ComplexCartOperations_SafeCheckout_Success()
        {
            // B1: Thêm 3 sản phẩm vào giỏ
            await _controller.AddToCart(productId: 2, quantity: 1); // Sản phẩm A
            await _controller.AddToCart(productId: 22, quantity: 1); // Sản phẩm B
            await _controller.AddToCart(productId: 3, quantity: 1); // Sản phẩm C

            var cartItems = await _context.CartItems.ToListAsync();
            Assert.Equal(3, cartItems.Count);

            var itemA = cartItems.First(x => x.ProductId == 2);
            var itemB = cartItems.First(x => x.ProductId == 22);
            var itemC = cartItems.First(x => x.ProductId == 3);

            // B2: Nhấn "X" để xóa sản phẩm C (ID 3)
            var deleteResult = await _controller.DeleteCartItem(itemC.CartItemId);
            var deleteJson = Assert.IsType<JsonResult>(deleteResult);
            Assert.True((bool)deleteJson.Value!.GetType().GetProperty("success")!.GetValue(deleteJson.Value));

            // B3: Bấm "+" để tăng sản phẩm B (ID 22) từ 1 lên 2 (đồng bộ Safe Checkout ngầm)
            var updateResult = await _controller.UpdateQuantity(itemB.CartItemId, 2);
            var updateJson = Assert.IsType<JsonResult>(updateResult);
            Assert.True((bool)updateJson.Value!.GetType().GetProperty("success")!.GetValue(updateJson.Value));

            // B4 & B5: Kiểm tra Database sau khi đồng bộ
            var finalCartItems = await _context.CartItems.Include(x => x.Product).ToListAsync();
            Assert.Equal(2, finalCartItems.Count); // Chỉ còn 2 sản phẩm A và B

            var remainingA = finalCartItems.First(x => x.ProductId == 2);
            var remainingB = finalCartItems.First(x => x.ProductId == 22);

            Assert.Equal(1, remainingA.Quantity);
            Assert.Equal(2, remainingB.Quantity);

            // Tổng giá trị = 1 * 45,990,000 + 2 * 19,990,000 = 85,970,000đ
            decimal expectedTotal = 1 * 45990000m + 2 * 19990000m;
            decimal actualTotal = finalCartItems.Sum(x => x.Quantity * x.Product.UnitPrice);
            Assert.Equal(expectedTotal, actualTotal);
        }
    }
}
