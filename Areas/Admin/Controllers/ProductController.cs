using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PawVerse.Models; 
using Microsoft.EntityFrameworkCore;
using PawVerse.Data; 
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PawVerse.Areas.Admin.Controllers
{
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ApplicationDbContext _context;

    public ProductController(ApplicationDbContext db)
    {
        _db = db;
        _context = db;
    }

    // Hiển thị danh sách sản phẩm
    public async Task<IActionResult> Index()
    {
        var products = await _db.SanPhams
            .Include(p => p.IdDanhMucNavigationIdDanhMucNavigation)  // Load related category using the correct navigation property
            .Include(p => p.IdThuongHieuNavigationIdThuongHieuNavigation) // Load related brand using the correct navigation property
            .AsNoTracking()
            .ToListAsync();
            
        return View(products);
    }

    // Xem chi tiết sản phẩm
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _db.SanPhams
            .Include(p => p.IdDanhMucNavigation)
            .Include(p => p.IdThuongHieuNavigation)
            .FirstOrDefaultAsync(m => m.IdSanPham == id);
            
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    // Thêm sản phẩm
   public IActionResult Create()
        {
            // Populate dropdown lists for DanhMuc and ThuongHieu
            ViewData["DanhMucList"] = new SelectList(_context.DanhMucs, "IdDanhMuc", "TenDanhMuc");
            ViewData["ThuongHieuList"] = new SelectList(_context.ThuongHieus, "IdThuongHieu", "TenThuongHieu");
            return View();
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenSanPham,TenAlias,IdDanhMuc,IdThuongHieu,TrongLuong,MauSac,XuatXu,MoTa,SoLuongTonKho,GiaBan,GiaVon,GiaKhuyenMai,NgaySanXuat,HanSuDung,TrangThai")] SanPham sanPham, IFormFile imageFile)
        {
            // Load related entities to ensure they exist
            var danhMuc = await _context.DanhMucs.FindAsync(sanPham.IdDanhMuc);
            var thuongHieu = await _context.ThuongHieus.FindAsync(sanPham.IdThuongHieu);
            
            // Always repopulate dropdown lists for the view
            ViewData["DanhMucList"] = new SelectList(_context.DanhMucs, "IdDanhMuc", "TenDanhMuc", sanPham.IdDanhMuc);
            ViewData["ThuongHieuList"] = new SelectList(_context.ThuongHieus, "IdThuongHieu", "TenThuongHieu", sanPham.IdThuongHieu);
            
            // Basic validation
            if (sanPham.IdDanhMuc <= 0 || danhMuc == null)
            {
                ModelState.AddModelError("IdDanhMuc", danhMuc == null ? "Danh mục không tồn tại" : "Vui lòng chọn danh mục");
            }
            
            if (sanPham.IdThuongHieu <= 0 || thuongHieu == null)
            {
                ModelState.AddModelError("IdThuongHieu", thuongHieu == null ? "Thương hiệu không tồn tại" : "Vui lòng chọn thương hiệu");
                return View(sanPham);
            }
            
            // Manual validation for required fields
            if (string.IsNullOrWhiteSpace(sanPham.TenSanPham))
            {
                ModelState.AddModelError("TenSanPham", "Tên sản phẩm là bắt buộc");
            }

            // If there are any model state errors, return the view with errors
            if (!ModelState.IsValid)
            {
                return View(sanPham);
            }
            
            try
            {
                // Set default values
                sanPham.NgayTao = DateTime.Now;
                sanPham.NgayCapNhat = DateTime.Now;
                sanPham.SoLanXem = 0;
                sanPham.SoLuongDaBan = 0;
                
                // Set the main foreign keys
                sanPham.IdDanhMuc = danhMuc.IdDanhMuc;
                sanPham.IdThuongHieu = thuongHieu.IdThuongHieu;
                
                // Set the additional foreign key columns
                sanPham.IdDanhMucNavigationIdDanhMuc = danhMuc.IdDanhMuc;
                sanPham.IdThuongHieuNavigationIdThuongHieu = thuongHieu.IdThuongHieu;
                
                // Set the navigation properties to null to prevent EF from trying to insert them
                sanPham.IdDanhMucNavigation = null;
                sanPham.IdThuongHieuNavigation = null;
                sanPham.IdDanhMucNavigationIdDanhMucNavigation = null;
                sanPham.IdThuongHieuNavigationIdThuongHieuNavigation = null;
                
                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("HinhAnh", "Chỉ chấp nhận file ảnh (JPG, JPEG, PNG, GIF)");
                        return View(sanPham);
                    }
                    else if (imageFile.Length > 5 * 1024 * 1024) // 5MB
                    {
                        ModelState.AddModelError("HinhAnh", "Kích thước file quá lớn (tối đa 5MB)");
                        return View(sanPham);
                    }
                    else
                    {
                        // Generate a unique file name
                        var uploadsFolder = Path.Combine("uploads", "products");
                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var relativePath = Path.Combine(uploadsFolder, fileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                        
                        // Ensure the directory exists
                        var directory = Path.GetDirectoryName(filePath);
                        if (directory != null && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        // Save the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        
                        // Update the image path in the model
                        sanPham.HinhAnh = $"/{relativePath.Replace("\\", "/")}";
                    }
                }
                else
                {
                    // Set a default image if no image is uploaded
                    sanPham.HinhAnh = "/images/default-product.png";
                }
                
                _context.Add(sanPham);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Log the database error with full details
                var errorMessage = $"Database error creating product: {ex.Message}";
                var innerException = ex.InnerException;
                
                while (innerException != null)
                {
                    errorMessage += $"\nInner Exception: {innerException.Message}";
                    innerException = innerException.InnerException;
                }
                
                Console.WriteLine(errorMessage);
                
                // Add detailed error to ModelState
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", $"Chi tiết: {ex.InnerException.Message}");
                }
                
                // Repopulate dropdown lists
                ViewData["DanhMucList"] = new SelectList(_context.DanhMucs, "IdDanhMuc", "TenDanhMuc", sanPham?.IdDanhMuc);
                ViewData["ThuongHieuList"] = new SelectList(_context.ThuongHieus, "IdThuongHieu", "TenThuongHieu", sanPham?.IdThuongHieu);
                
                return View(sanPham);
            }
            catch (Exception ex)
            {
                // Log other errors
                Console.WriteLine($"Error creating product: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu sản phẩm. Vui lòng thử lại.");
                
                // Repopulate dropdown lists
                ViewData["DanhMucList"] = new SelectList(_context.DanhMucs, "IdDanhMuc", "TenDanhMuc", sanPham?.IdDanhMuc);
                ViewData["ThuongHieuList"] = new SelectList(_context.ThuongHieus, "IdThuongHieu", "TenThuongHieu", sanPham?.IdThuongHieu);

                return View(sanPham);
            }
        }

    // Sửa sản phẩm
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _db.SanPhams
            .Include(p => p.IdDanhMucNavigation)
            .Include(p => p.IdThuongHieuNavigation)
            .FirstOrDefaultAsync(p => p.IdSanPham == id);
            
        if (product == null)
        {
            return NotFound();
        }

        // Load dropdown lists
        ViewData["DanhMucList"] = new SelectList(_context.DanhMucs, "IdDanhMuc", "TenDanhMuc", product.IdDanhMuc);
        ViewData["ThuongHieuList"] = new SelectList(_context.ThuongHieus, "IdThuongHieu", "TenThuongHieu", product.IdThuongHieu);
        
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdSanPham,TenSanPham,TenAlias,IdDanhMuc,IdThuongHieu,TrongLuong,MauSac,XuatXu,MoTa,SoLuongTonKho,SoLuongDaBan,GiaBan,GiaVon,GiaKhuyenMai,NgaySanXuat,HanSuDung,TrangThai,HinhAnh")] SanPham product, IFormFile? fileUpload)
    {
        if (id != product.IdSanPham)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Get the existing product from the database
                var existingProduct = await _db.SanPhams.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Handle image upload if a new file is provided
                if (fileUpload != null && fileUpload.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingProduct.HinhAnh))
                    {
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingProduct.HinhAnh.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }


                    // Save the new image
                    var uploadsFolder = Path.Combine("uploads", "products");
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileUpload.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", uploadsFolder, uniqueFileName);
                    
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await fileUpload.CopyToAsync(fileStream);
                    }
                    
                    product.HinhAnh = Path.Combine("/", uploadsFolder, uniqueFileName).Replace("\\", "/");
                }
                else
                {
                    // Keep the existing image if no new file is uploaded
                    product.HinhAnh = existingProduct.HinhAnh;
                }


                // Update the product properties
                existingProduct.TenSanPham = product.TenSanPham;
                existingProduct.TenAlias = product.TenAlias;
                existingProduct.IdDanhMuc = product.IdDanhMuc;
                existingProduct.IdThuongHieu = product.IdThuongHieu;
                existingProduct.TrongLuong = product.TrongLuong;
                existingProduct.MauSac = product.MauSac;
                existingProduct.XuatXu = product.XuatXu;
                existingProduct.MoTa = product.MoTa;
                existingProduct.SoLuongTonKho = product.SoLuongTonKho;
                existingProduct.GiaBan = product.GiaBan;
                existingProduct.GiaVon = product.GiaVon;
                existingProduct.GiaKhuyenMai = product.GiaKhuyenMai;
                existingProduct.NgaySanXuat = product.NgaySanXuat;
                existingProduct.HanSuDung = product.HanSuDung;
                existingProduct.TrangThai = product.TrangThai;
                existingProduct.HinhAnh = product.HinhAnh?.Replace("//", "/");
                existingProduct.NgayCapNhat = DateTime.Now;

                _db.Update(existingProduct);
                await _db.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.SanPhams.Any(e => e.IdSanPham == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        
        // If we got this far, something failed; redisplay form with validation errors
        ViewData["DanhMucList"] = new SelectList(_context.DanhMucs, "IdDanhMuc", "TenDanhMuc", product.IdDanhMuc);
        ViewData["ThuongHieuList"] = new SelectList(_context.ThuongHieus, "IdThuongHieu", "TenThuongHieu", product.IdThuongHieu);
        
        return View(product);
    }

    // Xóa sản phẩm
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.SanPhams.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _db.SanPhams.FindAsync(id);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để xóa.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // First, check if the product is in any order details.
            var isInOrderDetail = await _db.ChiTietDonHangs.AnyAsync(od => od.IdSanPham == id);
            if (isInOrderDetail)
            {
                TempData["ErrorMessage"] = "Không thể xóa sản phẩm này vì đã có trong đơn hàng của khách.";
                return RedirectToAction(nameof(Index));
            }

            _db.SanPhams.Remove(product);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Sản phẩm đã được xóa thành công.";
        }
        catch (DbUpdateException ex)
        {
            // This will catch foreign key constraint violations among other things.
            TempData["ErrorMessage"] = "Không thể xóa sản phẩm này vì có liên quan đến dữ liệu khác (ví dụ: đơn hàng). Vui lòng kiểm tra lại.";
            // Optional: Log the detailed exception for debugging
            Console.WriteLine(ex.ToString());
        }
        
        return RedirectToAction(nameof(Index));
    }
}
}
//Tmd1eeG7hW4gSOG6o2kgxJDEg25nIC0gQsO5aSBC4bqjbyBIw6JuIA==