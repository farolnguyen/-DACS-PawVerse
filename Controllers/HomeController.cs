using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PawVerse.Models;
using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PawVerse.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Lấy 3 thương hiệu nổi bật
        var brandNames = new[] { "Royal Canin", "Ferplast", "Catit" };
        var featuredBrands = await _context.ThuongHieus
            .Where(b => brandNames.Contains(b.TenThuongHieu))
            .ToListAsync();

        ViewData["FeaturedBrands"] = featuredBrands;

        // Lấy ID và số lượng sản phẩm cho các danh mục cố định một cách hiệu quả
        var categoryNames = new[] { "Phụ kiện & Đồ chơi", "Thực phẩm", "Nội thất", "Trang phục" };
        var categoryData = await _context.DanhMucs
            .Where(d => categoryNames.Contains(d.TenDanhMuc))
            .Select(d => new
            {
                Id = d.IdDanhMuc,
                Name = d.TenDanhMuc,
                ProductCount = d.SanPhams.Count()
            })
            .ToListAsync();

        // Ánh xạ dữ liệu vào ViewData để View sử dụng
        var toyData = categoryData.FirstOrDefault(c => c.Name == "Phụ kiện & Đồ chơi");
        ViewData["ToyCategoryId"] = toyData?.Id ?? 0;
        ViewData["ToyProductCount"] = toyData?.ProductCount ?? 0;

        var foodData = categoryData.FirstOrDefault(c => c.Name == "Thực phẩm");
        ViewData["FoodCategoryId"] = foodData?.Id ?? 0;
        ViewData["FoodProductCount"] = foodData?.ProductCount ?? 0;

        var interiorData = categoryData.FirstOrDefault(c => c.Name == "Nội thất");
        ViewData["InteriorCategoryId"] = interiorData?.Id ?? 0;
        ViewData["InteriorProductCount"] = interiorData?.ProductCount ?? 0;

        var fashionData = categoryData.FirstOrDefault(c => c.Name == "Trang phục");
        ViewData["FashionCategoryId"] = fashionData?.Id ?? 0;
        ViewData["FashionProductCount"] = fashionData?.ProductCount ?? 0;

        // Lấy tối đa 8 sản phẩm cho khu vực "Bán chạy nhất"
        var bestsellerProducts = await _context.SanPhams.Take(8).ToListAsync();

        return View(bestsellerProducts);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult Service()
    {
        return View();
    }

    public IActionResult HotelService()
    {
        return View();
    }

    public IActionResult SpaService()
    {
        return View();
    }

    public IActionResult OtherService()
    {
        return View();
    }

    public IActionResult Blog()
    {
        return View();
    }
}
