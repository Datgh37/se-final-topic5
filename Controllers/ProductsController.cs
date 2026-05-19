using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUITopic5_Team4.Data;
using WebUITopic5_Team4.Helpers;
using WebUITopic5_Team4.Models;
using WebUITopic5_Team4.Models.ViewModels;

namespace WebUITopic5_Team4.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ElectronicShopContext _context;

        public ProductsController(ElectronicShopContext context)
        {
            _context = context;
        }

        // GET: /Products
        [HttpGet]
        public async Task<IActionResult> Index(ProductIndexQueryViewModel query)
        {
            var model = await GetFilteredProductsModel(query);
            return View(model);
        }

        // GET: /Products/Filter (AJAX Partial)
        [HttpGet]
        public async Task<IActionResult> Filter(ProductIndexQueryViewModel query)
        {
            var model = await GetFilteredProductsModel(query);
            return PartialView("_ProductListPartial", model);
        }

        // GET: /Products/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Related products (same category, excluding current product)
            var related = await _context.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id)
                .Take(4)
                .ProjectToCard()
                .ToListAsync();

            ViewBag.RelatedProducts = related;

            return View(product);
        }

        // GET: /Products/LiveSearch?keyword=...
        [HttpGet]
        public async Task<IActionResult> LiveSearch(string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || keyword.Trim().Length < 2)
            {
                return Json(new List<object>());
            }

            var results = await _context.Products
                .AsNoTracking()
                .Where(p => p.ProductName.Contains(keyword))
                .Take(5)
                .Select(p => new
                {
                    productId = p.ProductId,
                    productName = p.ProductName,
                    unitPrice = p.UnitPrice,
                    discount = 0,
                    imageUrl = p.ImageUrl ?? "~/images/Others/product-1.jpg"
                })
                .ToListAsync();

            return Json(results);
        }

        // POST: /Products/ToggleFavorite
        [HttpPost]
        public IActionResult ToggleFavorite(int productId)
        {
            // Simple mock implementation of wishlist because we don't have a specific table for it in the schema,
            // or just save it in session for demo purposes!
            if (!User.Identity!.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này." });
            }

            var wishlist = HttpContext.Session.GetObjectFromJson<List<int>>("Wishlist") ?? new List<int>();
            bool isAdded = false;

            if (wishlist.Contains(productId))
            {
                wishlist.Remove(productId);
                isAdded = false;
            }
            else
            {
                wishlist.Add(productId);
                isAdded = true;
            }

            HttpContext.Session.SetObjectAsJson("Wishlist", wishlist);

            return Json(new { 
                success = true, 
                isAdded = isAdded, 
                totalCount = wishlist.Count, 
                message = isAdded ? "Đã thêm vào danh sách yêu thích" : "Đã xóa khỏi danh sách yêu thích" 
            });
        }

        // GET: /Products/GetFavoriteCount
        [HttpGet]
        public IActionResult GetFavoriteCount()
        {
            var wishlist = HttpContext.Session.GetObjectFromJson<List<int>>("Wishlist") ?? new List<int>();
            return Json(wishlist.Count);
        }

        // Helper to query and filter products
        private async Task<ProductIndexViewModel> GetFilteredProductsModel(ProductIndexQueryViewModel query)
        {
            var productsQuery = _context.Products.AsNoTracking().AsQueryable();

            // 1. Filtering
            if (query.CategoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == query.CategoryId.Value);
            }

            if (!string.IsNullOrEmpty(query.Keyword))
            {
                productsQuery = productsQuery.Where(p => p.ProductName.Contains(query.Keyword));
            }

            if (query.MinPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.UnitPrice >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.UnitPrice <= query.MaxPrice.Value);
            }

            // Get total count before pagination
            int totalProducts = await productsQuery.CountAsync();

            // 2. Sorting
            if (!string.IsNullOrEmpty(query.Sort))
            {
                switch (query.Sort.ToLower())
                {
                    case "price-asc":
                        productsQuery = productsQuery.OrderBy(p => p.UnitPrice);
                        break;
                    case "price-desc":
                        productsQuery = productsQuery.OrderByDescending(p => p.UnitPrice);
                        break;
                    case "name-asc":
                        productsQuery = productsQuery.OrderBy(p => p.ProductName);
                        break;
                    case "name-desc":
                        productsQuery = productsQuery.OrderByDescending(p => p.ProductName);
                        break;
                    default:
                        productsQuery = productsQuery.OrderByDescending(p => p.ProductId);
                        break;
                }
            }
            else
            {
                productsQuery = productsQuery.OrderByDescending(p => p.ProductId);
            }

            // 3. Pagination
            int page = query.Page > 0 ? query.Page : 1;
            int pageSize = query.PageSize > 0 ? query.PageSize : 12;

            var productsList = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectToCard()
                .ToListAsync();

            // Mock favorites status based on wishlist in session
            var wishlist = HttpContext.Session.GetObjectFromJson<List<int>>("Wishlist") ?? new List<int>();
            foreach (var item in productsList)
            {
                if (wishlist.Contains(item.ProductId))
                {
                    item.IsFavorite = true;
                }
            }

            // Get Sale Off products (for catalog carousel)
            var saleOff = await _context.Products
                .AsNoTracking()
                .Take(6)
                .ProjectToCard()
                .ToListAsync();

            return new ProductIndexViewModel
            {
                Query = query,
                Products = productsList,
                SaleOffProducts = saleOff,
                PriceFilter = new PriceFilterViewModel
                {
                    MinPrice = query.MinPrice ?? 0,
                    MaxPrice = query.MaxPrice ?? 200000000 // 200M VND default max
                },
                Pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalProducts,
                    TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize)
                }
            };
        }
    }
}
