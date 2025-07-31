using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using PawVerse.Models;
using PawVerse.Models.ViewModels;
using PawVerse.Services.Interfaces;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using PawVerse.Models.Location; // Added for AddressSelectionModel
using System.Diagnostics; // Added for Debug.WriteLine
using QuestPDF.Infrastructure; // For IDocument and related QuestPDF functionalities
using PawVerse.Services.PdfGeneration; // For InvoiceDocument
using QuestPDF.Fluent;

namespace PawVerse.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILocationService _locationService;

        public OrderController(ApplicationDbContext context, 
                               UserManager<ApplicationUser> userManager, 
                               ILocationService locationService)
        {
            _context = context;
            _userManager = userManager;
            _locationService = locationService;
        }

        // GET: Order/Checkout
        public async Task<IActionResult> Checkout()
        {
            // Logic to be filled: Get cart, prepare CheckoutViewModel
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge(); // or RedirectToAction("Login", "Account")
            }

            var cart = await _context.GioHangs
                .Include(c => c.GioHangChiTiets)
                .ThenInclude(ci => ci.SanPham)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.GioHangChiTiets.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Challenge(); // Or handle appropriately
            }

            var vanChuyenOptions = await _context.VanChuyens
                .OrderBy(vc => vc.TenVanChuyen)
                .Select(vc => new SelectListItem
                {
                    Value = vc.IdVanChuyen.ToString(),
                    Text = $"{vc.TenVanChuyen} ({vc.ThoiGianGiaoHang}) - {vc.PhiVanChuyen:N0} VNĐ"
                }).ToListAsync();

            var checkoutViewModel = new CheckoutViewModel
            {
                FullName = user?.FullName,
                PhoneNumber = user?.PhoneNumber,
                AddressInfo = new AddressSelectionModel { Provinces = new List<SelectListItem>() },
                AvailableVanChuyenOptions = vanChuyenOptions,
                CartItems = cart.GioHangChiTiets.Select(ci => new CartItemViewModel
                {
                    ProductId = ci.SanPhamId,
                    ProductName = ci.SanPham.TenSanPham,
                    ImageUrl = ci.SanPham.HinhAnh,
                    Price = (ci.SanPham.GiaKhuyenMai.HasValue && ci.SanPham.GiaKhuyenMai > 0) ? ci.SanPham.GiaKhuyenMai.Value : ci.SanPham.GiaBan,
                    Quantity = ci.SoLuong,
                    TotalPrice = ((ci.SanPham.GiaKhuyenMai.HasValue && ci.SanPham.GiaKhuyenMai > 0) ? ci.SanPham.GiaKhuyenMai.Value : ci.SanPham.GiaBan) * ci.SoLuong
                }).ToList()
            };

            checkoutViewModel.SubTotal = checkoutViewModel.CartItems.Sum(item => item.TotalPrice);
            // checkoutViewModel.ShippingFee = 0; // Set a default or calculate later
            checkoutViewModel.Total = checkoutViewModel.SubTotal + checkoutViewModel.ShippingFee;
            
            // Load provinces into the model
            var provincesFromService = await _locationService.GetProvincesAsync();
            if (checkoutViewModel.AddressInfo.Provinces != null && provincesFromService != null)
            {
                checkoutViewModel.AddressInfo.Provinces = provincesFromService.Select(p => new SelectListItem { Value = p.Code.ToString(), Text = p.Name }).ToList();
            }


            return View(checkoutViewModel);
        }

        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var cart = await _context.GioHangs
                .Include(c => c.GioHangChiTiets)
                .ThenInclude(ci => ci.SanPham)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.GioHangChiTiets.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng của bạn trống.");
                return RedirectToAction("Index", "Cart");
            }

            // --- SERVER-SIDE VALIDATION ---
            // First, check if required IDs are provided to prevent invalid API calls
            if (string.IsNullOrEmpty(model.AddressInfo.SelectedProvinceCode))
            {
                ModelState.AddModelError("AddressInfo.SelectedProvinceCode", "Vui lòng chọn Tỉnh/Thành phố.");
            }
            if (string.IsNullOrEmpty(model.AddressInfo.SelectedDistrictCode))
            {
                ModelState.AddModelError("AddressInfo.SelectedDistrictCode", "Vui lòng chọn Quận/Huyện.");
            }
            if (string.IsNullOrEmpty(model.AddressInfo.SelectedWardCode))
            {
                ModelState.AddModelError("AddressInfo.SelectedWardCode", "Vui lòng chọn Phường/Xã.");
            }
            if (model.SelectedVanChuyenId == 0)
            {
                ModelState.AddModelError("SelectedVanChuyenId", "Vui lòng chọn phương thức vận chuyển.");
            }

            // If any of the required fields are missing, stop now.
            if (!ModelState.IsValid)
            {
                await PopulateViewModelForError(model, cart);
                return View(model);
            }

            // Now that we have IDs, we can safely call the services
            var province = await _locationService.GetProvinceByCodeAsync(model.AddressInfo.SelectedProvinceCode);
            var district = await _locationService.GetDistrictByCodeAsync(model.AddressInfo.SelectedDistrictCode);
            var shippingMethod = await _context.VanChuyens.FindAsync(model.SelectedVanChuyenId);

            // Find the ward from the district's ward list
            var ward = district?.Wards?.FirstOrDefault(w => w.Code.ToString() == model.AddressInfo.SelectedWardCode);

            // Validate that the returned objects are not null
            if (province == null)
            {
                ModelState.AddModelError("AddressInfo.SelectedProvinceCode", "Tỉnh/Thành phố không hợp lệ.");
            }
            if (district == null)
            {
                ModelState.AddModelError("AddressInfo.SelectedDistrictCode", "Quận/Huyện không hợp lệ.");
            }
            if (ward == null) // Check if ward was found in the district's list
            {
                ModelState.AddModelError("AddressInfo.SelectedWardCode", "Phường/Xã không hợp lệ hoặc không thuộc Quận/Huyện đã chọn.");
            }
            if (shippingMethod == null)
            {
                ModelState.AddModelError("SelectedVanChuyenId", "Phương thức vận chuyển không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewModelForError(model, cart);
                return View(model);
            }

            // --- TRANSACTION --- 
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var order = new DonHang
                    {
                        IdNguoiDung = userId,
                        NgayDatHang = DateTime.Now,
                        TrangThai = "Chờ xác nhận",
                        TenKhachHang = model.FullName,
                        SoDienThoai = model.PhoneNumber,
                        DiaChiGiaoHang = $"{model.AddressInfo.StreetAddress}, {ward.Name}, {district.Name}, {province.Name}",
                        GhiChu = model.Note,
                        IdVanChuyen = shippingMethod.IdVanChuyen,
                        PhiVanChuyen = shippingMethod.PhiVanChuyen,
                        TongTien = cart.GioHangChiTiets.Sum(ci => ((ci.SanPham.GiaKhuyenMai.HasValue && ci.SanPham.GiaKhuyenMai > 0) ? ci.SanPham.GiaKhuyenMai.Value : ci.SanPham.GiaBan) * ci.SoLuong) + shippingMethod.PhiVanChuyen,
                        PhuongThucThanhToan = model.PaymentMethod // Lấy giá trị từ form
                    };

                    _context.DonHangs.Add(order);
                    await _context.SaveChangesAsync(); // Save to get OrderId

                    foreach (var item in cart.GioHangChiTiets)
                    {
                        var product = await _context.SanPhams.FindAsync(item.SanPhamId);
                        if (product == null || product.SoLuongTonKho < item.SoLuong)
                        {
                            ModelState.AddModelError("", $"Sản phẩm '{item.SanPham.TenSanPham}' không đủ hàng.");
                            await transaction.RollbackAsync();
                            await PopulateViewModelForError(model, cart);
                            return View(model);
                        }

                        product.SoLuongTonKho -= item.SoLuong;
                        product.SoLuongDaBan += item.SoLuong;

                        var orderDetail = new ChiTietDonHang
                        {
                            //IdDonHang = order.IdDonHang, // EF Core will infer from navigation property
                            //IdSanPham = product.IdSanPham, // EF Core will infer from navigation property
                            SoLuong = item.SoLuong,
                            DonGia = (product.GiaKhuyenMai.HasValue && product.GiaKhuyenMai > 0) ? product.GiaKhuyenMai.Value : product.GiaBan,
                            IdDonHangNavigation = order,   // Set navigation property to the parent DonHang
                            SanPham = product            // Set navigation property to the fetched SanPham
                        };
                        // order.ChiTietDonHangs.Add(orderDetail); // Adding via navigation property on child should suffice
                        _context.ChiTietDonHangs.Add(orderDetail); // Ensure it's tracked by the context
                    }

                    _context.GioHangChiTiets.RemoveRange(cart.GioHangChiTiets);
                    _context.GioHangs.Remove(cart);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đặt hàng thành công! Mã đơn hàng của bạn là: " + order.IdDonHang;
                    return RedirectToAction("OrderConfirmation", new { orderId = order.IdDonHang });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Debug.WriteLine($"ERROR during checkout: {ex.Message}");
                    ModelState.AddModelError("", "Đã có lỗi xảy ra, vui lòng thử lại.");
                    await PopulateViewModelForError(model, cart);
                    return View(model);
                }
            }
        }

        private async Task PopulateViewModelForError(CheckoutViewModel model, GioHang cart)
        {
            model.CartItems = cart.GioHangChiTiets.Select(ci => new CartItemViewModel
            {
                ProductId = ci.SanPhamId,
                ProductName = ci.SanPham.TenSanPham,
                ImageUrl = ci.SanPham.HinhAnh,
                Price = (ci.SanPham.GiaKhuyenMai.HasValue && ci.SanPham.GiaKhuyenMai > 0) ? ci.SanPham.GiaKhuyenMai.Value : ci.SanPham.GiaBan,
                Quantity = ci.SoLuong,
                TotalPrice = ((ci.SanPham.GiaKhuyenMai.HasValue && ci.SanPham.GiaKhuyenMai > 0) ? ci.SanPham.GiaKhuyenMai.Value : ci.SanPham.GiaBan) * ci.SoLuong
            }).ToList();
            model.SubTotal = model.CartItems.Sum(item => item.TotalPrice);
            model.Total = model.SubTotal + model.ShippingFee;

            model.AvailableVanChuyenOptions = await _context.VanChuyens
                .OrderBy(vc => vc.TenVanChuyen)
                .Select(vc => new SelectListItem
                {
                    Value = vc.IdVanChuyen.ToString(),
                    Text = $"{vc.TenVanChuyen} ({vc.ThoiGianGiaoHang}) - {vc.PhiVanChuyen:N0} VNĐ"
                }).ToListAsync();

            var provinces = await _locationService.GetProvincesAsync();
            model.AddressInfo.Provinces = provinces?.Select(p => new SelectListItem { Value = p.Code.ToString(), Text = p.Name }).ToList() ?? new List<SelectListItem>();

            if (!string.IsNullOrEmpty(model.AddressInfo.SelectedProvinceCode))
            {
                var districts = await _locationService.GetDistrictsByProvinceAsync(model.AddressInfo.SelectedProvinceCode);
                model.AddressInfo.Districts = districts?.Select(d => new SelectListItem { Value = d.Code.ToString(), Text = d.Name }).ToList() ?? new List<SelectListItem>();
            }

            if (!string.IsNullOrEmpty(model.AddressInfo.SelectedDistrictCode))
            {
                var wards = await _locationService.GetWardsByDistrictAsync(model.AddressInfo.SelectedDistrictCode);
                model.AddressInfo.Wards = wards?.Select(w => new SelectListItem { Value = w.Code.ToString(), Text = w.Name }).ToList() ?? new List<SelectListItem>();
            }
        }

        // GET: Order/OrderConfirmation
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.DonHangs
                                .Include(o => o.ChiTietDonHangs)
                                .ThenInclude(od => od.SanPham)
                                .Include(o => o.VanChuyen)
                                .FirstOrDefaultAsync(o => o.IdDonHang == orderId && o.IdNguoiDung == userId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", "Home");
            }

            var confirmationViewModel = new OrderConfirmationViewModel
            {
                IdDonHang = order.IdDonHang,
                NgayDatHang = order.NgayDatHang,
                TenKhachHang = order.TenKhachHang,
                SoDienThoai = order.SoDienThoai,
                DiaChiGiaoHang = order.DiaChiGiaoHang,
                TenVanChuyen = order.VanChuyen?.TenVanChuyen,
                ThoiGianGiaoHangUocTinh = order.VanChuyen?.ThoiGianGiaoHang,
                PhiVanChuyen = order.PhiVanChuyen,
                PhuongThucThanhToan = order.PhuongThucThanhToan,
                TongTienDonHang = order.TongTien,
                GhiChu = order.GhiChu,
                TrangThaiDonHang = order.TrangThai,
                ChiTietDonHangs = order.ChiTietDonHangs.Select(od => new OrderItemViewModel
                {
                    SanPhamId = od.IdSanPham,
                    TenSanPham = od.SanPham.TenSanPham,
                    ImageUrl = od.SanPham.HinhAnh, // Assuming SanPham has an ImageUrl property
                    SoLuong = od.SoLuong,
                    DonGia = od.DonGia,
                    ThanhTien = od.SoLuong * od.DonGia
                }).ToList()
            };

            return View(confirmationViewModel);
        }

        // GET: Order/MyOrders (User's Order History)
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = await _context.DonHangs
                                     .Where(o => o.IdNguoiDung == userId)
                                     .OrderByDescending(o => o.NgayDatHang)
                                     .ToListAsync();

            var orderHistoryViewModels = orders.Select(o => new OrderHistoryViewModel
            {
                OrderId = o.IdDonHang,
                OrderDate = o.NgayDatHang,
                Status = o.TrangThai,
                TotalAmount = o.TongTien,
                ShippingAddress = o.DiaChiGiaoHang,
                PhoneNumber = o.SoDienThoai,
                PaymentMethod = o.PhuongThucThanhToan,
                Note = o.GhiChu,
                CanCancel = o.TrangThai == "Chờ xác nhận" // Basic cancellation logic
            }).ToList();

            return View(orderHistoryViewModels);
        }

        // GET: Order/Details/5 (Order Details)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.DonHangs
                .Include(o => o.NguoiDung) // Include user details if needed
                .Include(o => o.NguoiDung) 
                .Include(o => o.ChiTietDonHangs)
                .ThenInclude(od => od.SanPham)
                .Include(o => o.VanChuyen) // Ensure VanChuyen is included for TenVanChuyen and ThoiGianGiaoHangUocTinh
                .FirstOrDefaultAsync(m => m.IdDonHang == id && m.IdNguoiDung == userId);

            if (order == null)
            {
                return NotFound();
            }

            var confirmationViewModel = new OrderConfirmationViewModel
            {
                IdDonHang = order.IdDonHang,
                NgayDatHang = order.NgayDatHang,
                TenKhachHang = order.TenKhachHang,
                SoDienThoai = order.SoDienThoai,
                DiaChiGiaoHang = order.DiaChiGiaoHang,
                TenVanChuyen = order.VanChuyen?.TenVanChuyen, // Assuming VanChuyen is included or loaded if needed
                ThoiGianGiaoHangUocTinh = order.VanChuyen?.ThoiGianGiaoHang, // Assuming VanChuyen is included
                PhiVanChuyen = order.PhiVanChuyen,
                PhuongThucThanhToan = order.PhuongThucThanhToan,
                TongTienDonHang = order.TongTien,
                GhiChu = order.GhiChu,
                TrangThaiDonHang = order.TrangThai, // This will be translated in the view
                ChiTietDonHangs = order.ChiTietDonHangs.Select(od => new OrderItemViewModel
                {
                    SanPhamId = od.IdSanPham,
                    TenSanPham = od.SanPham.TenSanPham,
                    ImageUrl = od.SanPham.HinhAnh,
                    SoLuong = od.SoLuong,
                    DonGia = od.DonGia,
                    ThanhTien = od.SoLuong * od.DonGia
                }).ToList()
            };

            return View(confirmationViewModel);
        }

        // POST: Order/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge(); // Or redirect to login
            }

            var order = await _context.DonHangs
                                  .FirstOrDefaultAsync(o => o.IdDonHang == id && o.IdNguoiDung == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(MyOrders));
            }

            // Assuming status "Chờ xác nhận" (or "Pending" if stored in English) is the only cancellable state
            if (order.TrangThai.Equals("Chờ xác nhận", StringComparison.OrdinalIgnoreCase))
            {
                order.TrangThai = "Đã hủy"; // Or "Cancelled" if storing in English
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đơn hàng #{order.IdDonHang} đã được hủy thành công.";
                }
                catch (DbUpdateException ex)
                {
                    // Log the error (uncomment ex variable name and write a log.)
                    TempData["ErrorMessage"] = "Đã có lỗi xảy ra khi cập nhật trạng thái đơn hàng. Vui lòng thử lại.";
                    // Consider logging ex.ToString() for debugging
                }
            }
            else
            {
                TempData["ErrorMessage"] = $"Không thể hủy đơn hàng #{order.IdDonHang} ở trạng thái hiện tại ('{order.TrangThai}').";
            }

            return RedirectToAction(nameof(MyOrders));
        }

        // GET: Order/GenerateInvoicePdf/5
        public async Task<IActionResult> GenerateInvoicePdf(int orderId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Challenge();
                }

                var order = await _context.DonHangs
                    .Include(o => o.NguoiDung)
                    .Include(o => o.ChiTietDonHangs)
                        .ThenInclude(od => od.SanPham)
                    .Include(o => o.VanChuyen)
                    .FirstOrDefaultAsync(m => m.IdDonHang == orderId && m.IdNguoiDung == userId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.";
                    return RedirectToAction(nameof(MyOrders));
                }

                var normalizedStatus = order.TrangThai?.Trim().ToLowerInvariant();
                var allowedStatuses = new[] { "hoàn thành", "đã giao hàng", "completed", "delivered", "shipping", "shipped" };
                if (!allowedStatuses.Contains(normalizedStatus))
                {
                    TempData["Error"] = "Chỉ có thể xuất hóa đơn cho các đơn hàng đã hoàn thành.";
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }

                var document = new InvoiceDocument(order);
                byte[] pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf", $"HoaDon_PawVerse_{order.IdDonHang}.pdf");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An unhandled exception occurred while generating PDF for order {orderId}: {ex}");
                TempData["Error"] = "Đã có lỗi nghiêm trọng xảy ra khi tạo file PDF. Vui lòng kiểm tra log để biết chi tiết.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }
        }
    }
}
