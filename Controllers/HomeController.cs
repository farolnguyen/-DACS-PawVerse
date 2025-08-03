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

        // Lấy tất cả danh mục từ cơ sở dữ liệu
        var categories = await _context.DanhMucs
            .Select(d => new
            {
                Id = d.IdDanhMuc,
                Name = d.TenDanhMuc,
                Image = d.HinhAnh,
                ProductCount = d.SanPhams.Count()
            })
            .ToListAsync();

        // Đưa danh sách danh mục vào ViewData để View sử dụng
        ViewData["Categories"] = categories;
            
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
