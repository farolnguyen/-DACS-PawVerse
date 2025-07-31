using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using PawVerse.Models;
using PawVerse.Models.ViewModels.Admin; // Added for BrandIndexViewModel
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using System;

namespace PawVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BrandController : Controller // Changed from ThuongHieuController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BrandController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment) // Changed from ThuongHieuController
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Brand
        public async Task<IActionResult> Index()
        {
            var brands = await _context.ThuongHieus
                .OrderBy(th => th.TenThuongHieu)
                .Select(th => new BrandIndexViewModel
                {
                    Brand = th,
                    ProductCount = _context.SanPhams.Count(p => p.IdThuongHieu == th.IdThuongHieu)
                })
                .ToListAsync();
            return View(brands);
        }

        // GET: Admin/Brand/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Brand/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenThuongHieu,TenAlias,MoTa,Logo,TrangThai")] ThuongHieu thuongHieu, IFormFile? fileUpload)
        {
            if (ModelState.IsValid)
            {
                if (fileUpload != null && fileUpload.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "brands");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await fileUpload.CopyToAsync(fileStream);
                    }
                    thuongHieu.Logo = "/uploads/brands/" + uniqueFileName;
                }
                else
                {
                    thuongHieu.Logo = ""; // Or a default logo path
                }

                if (string.IsNullOrEmpty(thuongHieu.TrangThai))
                {
                    thuongHieu.TrangThai = "Hoạt động";
                }

                _context.Add(thuongHieu);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm thương hiệu thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(thuongHieu);
        }

        // GET: Admin/Brand/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var thuongHieu = await _context.ThuongHieus.FindAsync(id);
            if (thuongHieu == null)
            {
                return NotFound();
            }
            return View(thuongHieu);
        }

        // POST: Admin/Brand/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdThuongHieu,TenThuongHieu,TenAlias,MoTa,Logo,TrangThai")] ThuongHieu thuongHieu, IFormFile? fileUpload)
        {
            if (id != thuongHieu.IdThuongHieu)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingThuongHieu = await _context.ThuongHieus.AsNoTracking().FirstOrDefaultAsync(th => th.IdThuongHieu == id);
                    if (existingThuongHieu == null) return NotFound();

                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "brands");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        if (!string.IsNullOrEmpty(existingThuongHieu.Logo) && existingThuongHieu.Logo != thuongHieu.Logo)
                        {
                            var oldLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, existingThuongHieu.Logo.TrimStart('/'));
                            if (System.IO.File.Exists(oldLogoPath))
                            {
                                System.IO.File.Delete(oldLogoPath);
                            }
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(fileStream);
                        }
                        thuongHieu.Logo = "/uploads/brands/" + uniqueFileName;
                    }
                    else
                    {
                        thuongHieu.Logo = existingThuongHieu.Logo;
                    }

                    // Kiểm tra nếu thương hiệu bị vô hiệu hóa (TrangThai = "Ngừng hoạt động")
                    if (thuongHieu.TrangThai == "Ngừng hoạt động")
                    {
                        // Tìm tất cả sản phẩm thuộc thương hiệu này
                        var productsToUpdate = await _context.SanPhams
                            .Where(p => p.IdThuongHieu == thuongHieu.IdThuongHieu)
                            .ToListAsync();

                        // Cập nhật trạng thái của tất cả sản phẩm thành "Hết hàng"
                        foreach (var product in productsToUpdate)
                        {
                            product.TrangThai = "Hết hàng";
                            _context.Update(product);
                        }

                        // Thêm thông báo về số sản phẩm đã cập nhật
                        TempData["InfoMessage"] = $"Đã cập nhật {productsToUpdate.Count} sản phẩm thành trạng thái 'Hết hàng'.";
                    }

                    _context.Update(thuongHieu);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thương hiệu thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThuongHieuExists(thuongHieu.IdThuongHieu))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(thuongHieu);
        }

        // GET: Admin/Brand/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var thuongHieu = await _context.ThuongHieus
                .FirstOrDefaultAsync(m => m.IdThuongHieu == id);

            if (thuongHieu == null)
            {
                return NotFound();
            }

            ViewBag.Products = await _context.SanPhams
                                      .Where(p => p.IdThuongHieu == id)
                                      .OrderByDescending(p => p.IdSanPham) // Or any other preferred order
                                      .Take(10) // Example: Take latest 10 products
                                      .ToListAsync();
            ViewBag.ProductCount = await _context.SanPhams.CountAsync(p => p.IdThuongHieu == id);

            return View(thuongHieu);
        }

        // GET: Admin/Brand/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var thuongHieu = await _context.ThuongHieus
                .FirstOrDefaultAsync(m => m.IdThuongHieu == id);
            if (thuongHieu == null)
            {
                return NotFound();
            }
            ViewBag.ProductCount = await _context.SanPhams.CountAsync(p => p.IdThuongHieu == id);
            ViewBag.Products = await _context.SanPhams
                                     .Where(p => p.IdThuongHieu == id)
                                     .OrderByDescending(p => p.IdSanPham)
                                     .Take(5) // Show a few products on delete confirmation page if any
                                     .ToListAsync();
            return View(thuongHieu);
        }

        // POST: Admin/Brand/DeleteConfirmed/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) // Renamed from Delete to DeleteConfirmed for clarity if needed, or keep as Delete if matching a GET Delete view
        {
            var thuongHieu = await _context.ThuongHieus.FindAsync(id);
            if (thuongHieu != null)
            {
                var productsCount = await _context.SanPhams.CountAsync(p => p.IdThuongHieu == id);
                if (productsCount > 0)
                {
                    TempData["ErrorMessage"] = "Không thể xóa thương hiệu này vì có sản phẩm đang sử dụng.";
                    return RedirectToAction(nameof(Index));
                }

                if (!string.IsNullOrEmpty(thuongHieu.Logo))
                {
                    var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, thuongHieu.Logo.TrimStart('/'));
                    if (System.IO.File.Exists(logoPath))
                    {
                        System.IO.File.Delete(logoPath);
                    }
                }

                _context.ThuongHieus.Remove(thuongHieu);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa thương hiệu thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ThuongHieuExists(int id)
        {
            return _context.ThuongHieus.Any(e => e.IdThuongHieu == id);
        }
    }
}
