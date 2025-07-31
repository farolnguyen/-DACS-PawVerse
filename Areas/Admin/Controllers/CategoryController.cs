using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using PawVerse.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;

namespace PawVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoryController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Category
        public async Task<IActionResult> Index()
        {
            var categories = await _context.DanhMucs
                .OrderBy(c => c.TenDanhMuc)
                .ToListAsync();
            return View(categories);
        }

        // GET: Admin/Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMucs
                .FirstOrDefaultAsync(m => m.IdDanhMuc == id);
                
            if (danhMuc == null)
            {
                return NotFound();
            }


            return View(danhMuc);
        }

        // GET: Admin/Category/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenDanhMuc,MoTa,HinhAnh,TrangThai")] DanhMuc danhMuc, IFormFile? fileUpload)
        {
            if (ModelState.IsValid)
            {
                // Handle file upload
                if (fileUpload != null && fileUpload.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "categories");
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

                    danhMuc.HinhAnh = "/uploads/categories/" + uniqueFileName;
                }

                else
                {
                    danhMuc.HinhAnh = "";
                }

                // Set default status if not provided
                if (string.IsNullOrEmpty(danhMuc.TrangThai))
                {
                    danhMuc.TrangThai = "Hoạt động";
                }

                _context.Add(danhMuc);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(danhMuc);
        }

        // GET: Admin/Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMucs.FindAsync(id);
            if (danhMuc == null)
            {
                return NotFound();
            }
            return View(danhMuc);
        }

        // POST: Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdDanhMuc,TenDanhMuc,MoTa,HinhAnh,TrangThai")] DanhMuc danhMuc, IFormFile? fileUpload)
        {
            if (id != danhMuc.IdDanhMuc)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle file upload if a new file is provided
                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "categories");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(danhMuc.HinhAnh))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, danhMuc.HinhAnh.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileUpload.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(fileStream);
                        }

                        danhMuc.HinhAnh = "/uploads/categories/" + uniqueFileName;
                    }


                    // Kiểm tra nếu danh mục bị vô hiệu hóa (TrangThai = "Ngừng hoạt động")
                    if (danhMuc.TrangThai == "Ngừng hoạt động")
                    {
                        // Tìm tất cả sản phẩm thuộc danh mục này
                        var productsToUpdate = await _context.SanPhams
                            .Where(p => p.IdDanhMuc == danhMuc.IdDanhMuc)
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

                    _context.Update(danhMuc);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DanhMucExists(danhMuc.IdDanhMuc))
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
            return View(danhMuc);
        }

        // GET: Admin/Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMucs
                .FirstOrDefaultAsync(m => m.IdDanhMuc == id);
                
            if (danhMuc == null)
            {
                return NotFound();
            }


            return View(danhMuc);
        }


        // POST: Admin/Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var danhMuc = await _context.DanhMucs.FindAsync(id);
            if (danhMuc != null)
            {
                // Check if the category is being used by any products
                var productsCount = await _context.SanPhams.CountAsync(p => p.IdDanhMuc == id);
                if (productsCount > 0)
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục này vì có sản phẩm đang sử dụng.";
                    return RedirectToAction(nameof(Index));
                }

                // Delete the image file if it exists
                if (!string.IsNullOrEmpty(danhMuc.HinhAnh))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, danhMuc.HinhAnh.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }


                _context.DanhMucs.Remove(danhMuc);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DanhMucExists(int id)
        {
            return _context.DanhMucs.Any(e => e.IdDanhMuc == id);
        }
    }
}
