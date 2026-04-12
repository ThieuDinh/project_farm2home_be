using farm2homeWebApi.Data;
using farm2homeWebApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace farm2homeWebApi.Controllers
{
    [Route("products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // GET /products
        // Dùng cho: ProductsPage.jsx
        //
        // Query params:
        //   ?search=táo         → Tìm kiếm theo tên sản phẩm
        //   ?category=Rau củ    → Lọc theo tên danh mục (FE dùng string, không dùng ID)
        //   ?sortBy=price_asc   → Sắp xếp: price_asc | price_desc | name_asc | name_desc | newest
        //   ?page=1             → Trang hiện tại (mặc định: 1)
        //   ?pageSize=12        → Số sản phẩm/trang (mặc định: 12)
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,  // FE lọc bằng tên category (VD: "Rau củ")
            [FromQuery] string sortBy = "newest",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12
        )
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 12;

            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // Tìm kiếm theo tên (không phân biệt hoa thường)
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));

            // Lọc theo tên category - FE truyền "Rau củ", "Trái cây", "Ngũ cốc"
            // Nếu là "Tất cả" hoặc null thì bỏ qua filter
            if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
                query = query.Where(p => p.Category.Name == category);

            // Sắp xếp
            query = sortBy switch
            {
                "price_asc"  => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc"   => query.OrderBy(p => p.Name),
                "name_desc"  => query.OrderByDescending(p => p.Name),
                _            => query.OrderByDescending(p => p.Id), // newest (mặc định)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id           = p.Id,
                    Name         = p.Name,
                    Description  = p.Description,
                    Image        = p.Image,
                    Type         = p.Type,
                    Price        = p.Price,
                    Unit         = p.Unit,
                    Stock        = p.Stock,
                    CategoryId   = p.CategoryId,
                    CategoryName = p.Category.Name, // FE dùng product.category để hiển thị
                })
                .ToListAsync();

            return Ok(new ProductsPageResponse
            {
                Items      = items,
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize,
                TotalPages = totalPages,
            });
        }

        // ---------------------------------------------------------
        // GET /products/{id}
        // Dùng cho: ProductDetailPage.jsx
        //   useParams().id → gọi GET /products/{id}
        //   FE cần: name, price, category, image, description, unit, stock
        // ---------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Id == id)
                .Select(p => new ProductDto
                {
                    Id           = p.Id,
                    Name         = p.Name,
                    Description  = p.Description,
                    Image        = p.Image,
                    Type         = p.Type,
                    Price        = p.Price,
                    Unit         = p.Unit,
                    Stock        = p.Stock,
                    CategoryId   = p.CategoryId,
                    CategoryName = p.Category.Name,
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            return Ok(product);
        }

        // ---------------------------------------------------------
        // GET /products/categories
        // Dùng cho: ProductsPage.jsx sidebar danh mục
        //   const CATEGORIES = ["Tất cả", "Rau củ", "Trái cây", "Ngũ cốc"]
        //   → FE sẽ thêm "Tất cả" vào đầu, BE trả về danh sách còn lại
        // ---------------------------------------------------------
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id   = c.Id,
                    Name = c.Name,
                    ImageUrl = c.ImageUrl,
                    Description = c.Description,
                    ColorCode = c.ColorCode
                })
                .ToListAsync();

            return Ok(categories);
        }

        // ---------------------------------------------------------
        // GET /products/featured?limit=8
        // Dùng cho: Home.jsx section "Sản phẩm nổi bật"
        //   Hiện tại Home dùng mock data (sp1..sp8), khi tích hợp thật sẽ gọi API này
        // ---------------------------------------------------------
        [HttpGet("featured")]
        public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 8)
        {
            if (limit < 1 || limit > 50) limit = 8;

            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(limit)
                .Select(p => new ProductDto
                {
                    Id           = p.Id,
                    Name         = p.Name,
                    Description  = p.Description,
                    Image        = p.Image,
                    Type         = p.Type,
                    Price        = p.Price,
                    Unit         = p.Unit,
                    Stock        = p.Stock,
                    CategoryId   = p.CategoryId,
                    CategoryName = p.Category.Name,
                })
                .ToListAsync();

            return Ok(products);
        }

        // ---------------------------------------------------------
        // GET /products/search-suggestions?q=táo
        // Dùng cho: Thanh tìm kiếm autocomplete (nếu FE muốn thêm)
        //   Trả về danh sách tên sản phẩm gợi ý (tối đa 8)
        // ---------------------------------------------------------
        [HttpGet("search-suggestions")]
        public async Task<IActionResult> GetSearchSuggestions([FromQuery] string q = "")
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<string>());

            var suggestions = await _context.Products
                .Where(p => p.Name.Contains(q))
                .OrderBy(p => p.Name)
                .Take(8)
                .Select(p => new { p.Id, p.Name, p.Image, p.Price })
                .ToListAsync();

            return Ok(suggestions);
        }
    }
}
