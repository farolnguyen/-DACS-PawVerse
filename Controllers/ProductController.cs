using Microsoft.AspNetCore.Mvc;
using PawVerse.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PawVerse.Models;
using System.Collections.Generic;
using System.Linq;

namespace PawVerse.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductController(ApplicationDbContext context) => _context = context;

        public class ProductIndexViewModel
        {
            public IEnumerable<SanPham> Products { get; set; }
            public IEnumerable<DanhMuc> Categories { get; set; }
            public int? SelectedCategoryId { get; set; }
            public string SelectedCategoryName { get; set; }
            public bool ShowWishlist { get; set; } = false;
        }

        // /Product/Index
        public async Task<IActionResult> Index(int? categoryId = null, string brand = null, string priceRange = null, string sortBy = null, bool showWishlist = false, string searchTerm = null)
        {
            var products = _context.SanPhams.AsQueryable();
            var categories = await _context.DanhMucs.OrderBy(c => c.TenDanhMuc).ToListAsync();

            // Lấy danh sách thương hiệu duy nhất từ bảng ThuongHieu
            ViewBag.Brands = await _context.ThuongHieus
                .OrderBy(t => t.TenThuongHieu)
                .Select(t => t.TenThuongHieu)
                .Distinct()
                .ToListAsync();

            string selectedCategoryName = "Tất cả sản phẩm";
            var userId = User.Identity.IsAuthenticated ? _context.Users.FirstOrDefault(u => u.UserName == User.Identity.Name)?.Id : null;

            // Xử lý hiển thị danh sách yêu thích
            if (showWishlist && userId != null)
            {
                var wishlistProductIds = await _context.DanhSachYeuThiches
                    .Where(w => w.IdNguoiDung == userId)
                    .Select(w => w.IdSanPham)
                    .ToListAsync();
                
                products = products.Where(p => wishlistProductIds.Contains(p.IdSanPham));
                selectedCategoryName = "Sản phẩm yêu thích";
            }
            // Lọc theo danh mục nếu có
            else if (categoryId.HasValue)
            {
                products = products.Where(p => p.IdDanhMuc == categoryId);
                var category = categories.FirstOrDefault(c => c.IdDanhMuc == categoryId);
                if (category != null)
                {
                    selectedCategoryName = category.TenDanhMuc;
                }
            }

            // Lọc theo thương hiệu
            if (!string.IsNullOrEmpty(brand))
            {
                products = products.Where(p => p.IdThuongHieuNavigation != null && 
                                             p.IdThuongHieuNavigation.TenThuongHieu == brand);
            }
            
            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                products = products.Where(p => p.TenSanPham.ToLower().Contains(searchTerm) || 
                                           (p.MoTa != null && p.MoTa.ToLower().Contains(searchTerm)));
                selectedCategoryName = $"Kết quả tìm kiếm cho '{searchTerm}'";
            }

            // Lọc theo khoảng giá
            if (!string.IsNullOrEmpty(priceRange))
            {
                var priceRanges = priceRange.Split('-');
                if (priceRanges.Length == 2 && 
                    decimal.TryParse(priceRanges[0], out decimal minPrice) && 
                    decimal.TryParse(priceRanges[1], out decimal maxPrice))
                {
                    products = products.Where(p => p.GiaBan >= minPrice && p.GiaBan <= maxPrice);
                }
            }

            // Sắp xếp
            switch (sortBy)
            {
                case "name_asc":
                    products = products.OrderBy(p => p.TenSanPham);
                    break;
                case "name_desc":
                    products = products.OrderByDescending(p => p.TenSanPham);
                    break;
                case "price_asc":
                    products = products.OrderBy(p => p.GiaBan);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.GiaBan);
                    break;
                case "newest":
                    products = products.OrderByDescending(p => p.NgayTao);
                    break;
                default:
                    products = products.OrderBy(p => p.TenSanPham);
                    break;
            }

            var viewModel = new ProductIndexViewModel
            {
                Products = await products.ToListAsync(),
                Categories = categories,
                SelectedCategoryId = categoryId,
                SelectedCategoryName = selectedCategoryName
            };

            return View(viewModel);
        }

        // /Product/Info/5  (xem chi tiết)
        public async Task<IActionResult> Info(int id)
        {
            var sp = await _context.SanPhams
                .Include(p => p.IdThuongHieuNavigation)
                .FirstOrDefaultAsync(p => p.IdSanPham == id);
                
            if (sp == null)
            {
                return NotFound();
            }

            // Tăng số lượt xem
            sp.SoLanXem = sp.SoLanXem + 1;
            await _context.SaveChangesAsync();

            return View(sp);
        }

        // /Product/GetRelatedProducts?categoryId=1&excludeProductId=5
        [HttpGet]
        public async Task<IActionResult> GetRelatedProducts(int categoryId, int excludeProductId, int count = 4)
        {
            var relatedProducts = await _context.SanPhams
                .Where(p => p.IdDanhMuc == categoryId && p.IdSanPham != excludeProductId && p.SoLuongTonKho > 0)
                .OrderByDescending(p => p.NgayTao)
                .Take(count)
                .Select(p => new 
                { 
                    p.IdSanPham,
                    p.TenSanPham,
                    p.GiaBan,
                    p.GiaKhuyenMai,
                    p.HinhAnh,
                    p.SoLuongTonKho,
                    p.TenAlias
                })
                .ToListAsync();

            var productCards = relatedProducts.Select(p => 
                $@"<div class='col-md-3 mb-4'>
                    <div class='card h-100 product-card'>
                        <div class='position-relative'>
                            <a href='/Product/Info/{p.IdSanPham}'>
                                <img src='{p.HinhAnh ?? "/images/default-product.jpg"}' class='card-img-top' alt='{p.TenSanPham}'>
                            </a>
                            {GetProductBadge(p.SoLuongTonKho)}
                            {GetSaleBadge(p.GiaBan, p.GiaKhuyenMai)}
                        </div>
                        <div class='card-body'>
                            <h5 class='card-title'>
                                <a href='/Product/Info/{p.IdSanPham}' class='text-decoration-none text-dark'>{p.TenSanPham}</a>
                            </h5>
                            <div class='d-flex justify-content-between align-items-center'>
                                {GetPriceHtml(p.GiaBan, p.GiaKhuyenMai)}
                            </div>
                        </div>

                    </div>
                </div>");

            return Content(string.Join("", productCards), "text/html");
        }

        private string GetProductBadge(int? soLuongTonKho)
        {
            if (soLuongTonKho <= 0)
            {
                return "<span class='badge bg-danger position-absolute top-0 end-0 m-2'>Hết hàng</span>";
            }
            return "";
        }

        private string GetSaleBadge(decimal giaBan, decimal? giaKhuyenMai)
        {
            if (giaKhuyenMai.HasValue && giaKhuyenMai > 0)
            {
                var discountPercent = (int)((giaBan - giaKhuyenMai.Value) / giaBan * 100);
                return $"<span class='badge bg-warning text-dark position-absolute top-0 start-0 m-2'>-{discountPercent}%</span>";
            }
            return "";
        }

        private string GetPriceHtml(decimal giaBan, decimal? giaKhuyenMai)
        {
            if (giaKhuyenMai.HasValue && giaKhuyenMai > 0)
            {
                return string.Format(
                    "<div>" +
                    "<span class='text-danger fw-bold'>{0} ₫</span>" +
                    "<small class='text-muted text-decoration-line-through ms-2'>{1} ₫</small>" +
                    "</div>",
                    giaKhuyenMai.Value.ToString("N0"),
                    giaBan.ToString("N0"));
            }
            return $"<span class='fw-bold'>{giaBan.ToString("N0")} ₫</span>";
        }
    }
}