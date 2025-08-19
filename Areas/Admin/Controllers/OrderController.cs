using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using PawVerse.Models;
using PawVerse.Areas.Admin.ViewModels;
using QuestPDF.Fluent;
using PawVerse.Services.PdfGeneration;

namespace PawVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApplicationDbContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Order
        public async Task<IActionResult> Index(string status = "all")
        {
            ViewData["Breadcrumb"] = new List<string> { "Quản lý đơn hàng" };
            
            var query = _context.DonHangs
                .Include(dh => dh.NguoiDung)
                .OrderByDescending(dh => dh.NgayDatHang)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                string dbStatus = status;
                if (status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    dbStatus = "Chờ xác nhận";
                }
                else if (status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    dbStatus = "Đã hủy";
                }
                query = query.Where(dh => dh.TrangThai == dbStatus);
            }

            var orders = await query
                .Select(dh => new OrderListViewModel
                {
                    IdDonHang = dh.IdDonHang,
                    TenKhachHang = dh.TenKhachHang,
                    SoDienThoai = dh.SoDienThoai,
                    NgayDatHang = dh.NgayDatHang,
                    TrangThai = dh.TrangThai,
                    TongTien = dh.TongTien,
                    PhuongThucThanhToan = dh.PhuongThucThanhToan
                })
                .ToListAsync();

            ViewBag.Status = status;
            return View(orders);
        }

        // GET: Admin/Order/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donHang = await _context.DonHangs
                .Include(dh => dh.NguoiDung)
                .Include(dh => dh.IdCouponNavigation)
                .Include(dh => dh.VanChuyen)
                .Include(dh => dh.ChiTietDonHangs)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(dh => dh.IdDonHang == id);

            if (donHang == null)
            {
                return NotFound();
            }

            var viewModel = new OrderDetailViewModel
            {
                IdDonHang = donHang.IdDonHang,
                TenKhachHang = donHang.TenKhachHang,
                SoDienThoai = donHang.SoDienThoai,
                DiaChiGiaoHang = donHang.DiaChiGiaoHang,
                NgayDatHang = donHang.NgayDatHang,
                NgayGiaoHangDuKien = donHang.NgayGiaoHangDuKien,
                NgayHuy = donHang.NgayHuy,
                TrangThai = TranslateStatusToCode(donHang.TrangThai),
                PhuongThucThanhToan = donHang.PhuongThucThanhToan,
                TongTien = donHang.TongTien,
                CouponCode = donHang.IdCouponNavigation?.TenMaCoupon,
                GiamGia = donHang.IdCouponNavigation?.MucGiamGia,
                ThanhTien = donHang.TongTien - (donHang.IdCouponNavigation?.MucGiamGia ?? 0)
            };

            foreach (var item in donHang.ChiTietDonHangs)
            {
                viewModel.ChiTietDonHangs.Add(new OrderItemViewModel
                {
                    TenSanPham = item.SanPham.TenSanPham,
                    HinhAnh = !string.IsNullOrEmpty(item.SanPham.HinhAnh) 
                        ? item.SanPham.HinhAnh.Replace("//", "/") 
                        : "/images/default-product.png",
                    SoLuong = item.SoLuong,
                    DonGia = item.DonGia
                });
            }

            ViewData["Breadcrumb"] = new List<string> { 
                "Quản lý đơn hàng", 
                $"Đơn hàng #{donHang.IdDonHang}" 
            };

            return View(viewModel);
        }

        // GET: Admin/Order/GenerateInvoicePdf/5
        public async Task<IActionResult> GenerateInvoicePdf(int orderId)
        {
            try
            {
                _logger.LogInformation("Attempting to generate PDF for Order ID: {OrderId}", orderId);
                var order = await _context.DonHangs
                                    .Include(o => o.NguoiDung) // User details
                                    .Include(o => o.ChiTietDonHangs)
                                        .ThenInclude(cd => cd.SanPham) // Order items and their products
                                    .Include(o => o.VanChuyen) // Shipping info
                                    .FirstOrDefaultAsync(o => o.IdDonHang == orderId);

                _logger.LogInformation("Found order with status: {OrderStatus}", order?.TrangThai);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return NotFound();
                }

                var normalizedStatus = order.TrangThai?.Trim().ToLowerInvariant();
                var allowedStatuses = new[] { "hoàn thành", "đã giao hàng", "completed", "delivered", "shipping", "shipped" };
                if (!allowedStatuses.Contains(normalizedStatus))
                {
                    TempData["Error"] = "Chỉ có thể xuất hóa đơn cho các đơn hàng đã hoàn thành hoặc đã giao hàng.";
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }

                _logger.LogInformation("Generating PDF for order {OrderId} with status {OrderStatus}", orderId, order.TrangThai);

                var document = new InvoiceDocument(order);
                byte[] pdfBytes = document.GeneratePdf();
                _logger.LogInformation("Successfully generated {ByteCount} bytes for PDF of order {OrderId}", pdfBytes.Length, orderId);
                return File(pdfBytes, "application/pdf", $"HoaDon_PawVerse_{order.IdDonHang}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while generating PDF for order {OrderId}", orderId);
                TempData["Error"] = "Đã có lỗi nghiêm trọng xảy ra khi tạo file PDF. Vui lòng kiểm tra log để biết chi tiết.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }
        }

        // GET: Admin/Order/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null)
            {
                return NotFound();
            }

            ViewData["Breadcrumb"] = new List<string> { 
                "Quản lý đơn hàng", 
                $"Cập nhật trạng thái đơn hàng #{donHang.IdDonHang}" 
            };

            var viewModel = new UpdateOrderStatusViewModel
            {
                IdDonHang = donHang.IdDonHang,
                TrangThai = donHang.TrangThai,
            };

            return View(viewModel);
        }

        // POST: Admin/Order/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateOrderStatusViewModel viewModel)
        {
            if (id != viewModel.IdDonHang)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var donHang = await _context.DonHangs.FindAsync(id);
                    if (donHang == null)
                    {
                        return NotFound();
                    }

                    // Update status and related dates
                    donHang.TrangThai = viewModel.TrangThai;
                    
                    if (viewModel.TrangThai == "Đã hủy" && donHang.NgayHuy == null)
                    {
                        donHang.NgayHuy = DateTime.Now;
                    }
                    else if (viewModel.TrangThai == "Đang giao hàng" && donHang.NgayGiaoHangDuKien == null)
                    {
                        donHang.NgayGiaoHangDuKien = DateTime.Now.AddDays(3); // Default 3 days for delivery
                    }

                    _context.Update(donHang);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
                    return RedirectToAction(nameof(Details), new { id = donHang.IdDonHang });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonHangExists(viewModel.IdDonHang))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            ViewData["Breadcrumb"] = new List<string> { 
                "Quản lý đơn hàng", 
                $"Cập nhật trạng thái đơn hàng #{viewModel.IdDonHang}" 
            };
            
            return View(viewModel);
        }

        // POST: Admin/Order/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string ghiChu = null)
        {
            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null)
            {
                return NotFound();
            }

            // Only allow canceling orders that are not already completed or canceled
            if (donHang.TrangThai != "Hoàn thành" && donHang.TrangThai != "Đã hủy")
            {
                donHang.TrangThai = "Đã hủy";
                donHang.NgayHuy = DateTime.Now;
                
                // Add note if provided
                if (!string.IsNullOrEmpty(ghiChu))
                {
                    // You might want to save this note to a separate table
                    // For now, we'll just log it
                    _logger.LogInformation($"Order {id} was canceled. Note: {ghiChu}");
                }

                _context.Update(donHang);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng này do trạng thái hiện tại.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private bool DonHangExists(int id)
        {
            return _context.DonHangs.Any(e => e.IdDonHang == id);
        }

        private string TranslateStatusToCode(string vietnameseStatus)
        {
            return vietnameseStatus switch
            {
                "Chờ xác nhận" => "pending",
                "Đang xử lý" => "processing",
                "Đang giao hàng" => "shipping",
                "Hoàn thành" => "completed",
                "Đã hủy" => "cancelled",
                _ => vietnameseStatus.ToLower() // Fallback for any other status
            };
        }
    }
}
//Tmd1eeG7hW4gSOG6o2kgxJDEg25nIC0gQsO5aSBC4bqjbyBIw6JuIA==