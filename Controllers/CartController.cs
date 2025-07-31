using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using PawVerse.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PawVerse.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await GetOrCreateCartAsync(userId);
            
            // Lấy chi tiết giỏ hàng với thông tin sản phẩm
            var cartDetails = await _context.GioHangChiTiets
                .Include(cd => cd.SanPham)
                .ThenInclude(p => p.IdThuongHieuNavigation)
                .Where(cd => cd.GioHang.UserId == userId)
                .ToListAsync();

            // Đếm tổng số lượng sản phẩm trong giỏ hàng
            var totalItems = cartDetails.Sum(item => item.SoLuong);
            ViewData["CartItemCount"] = totalItems;

            return View(cartDetails);
        }



        [HttpPost]
        public async Task<IActionResult> AddToCart()
        {
            try
            {
                // Read form data
                var form = await Request.ReadFormAsync();
                
                if (!int.TryParse(form["productId"], out int productId) || productId <= 0)
                {
                    return Json(new { success = false, message = "ID sản phẩm không hợp lệ" });
                }

                if (!int.TryParse(form["quantity"], out int quantity) || quantity < 1)
                {
                    return Json(new { success = false, message = "Số lượng không hợp lệ" });
                }

                // Log request
                Console.WriteLine($"AddToCart request - ProductId: {productId}, Quantity: {quantity}");
                
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng" 
                    });
                }

                Console.WriteLine($"User ID: {userId}");

                var cart = await GetOrCreateCartAsync(userId);
                Console.WriteLine($"Cart ID: {cart?.Id}");
                
                // Kiểm tra sản phẩm có tồn tại không
                Console.WriteLine($"Looking for product with ID: {productId}");
                var product = await _context.SanPhams.FindAsync(productId);
                
                if (product == null) 
                {
                    // Log all product IDs for debugging
                    var allProductIds = await _context.SanPhams.Select(p => p.IdSanPham).ToListAsync();
                    Console.WriteLine($"Product with ID {productId} not found in database. Available IDs: {string.Join(", ", allProductIds)}");
                    
                    return Json(new { 
                        success = false, 
                        message = $"Không tìm thấy sản phẩm (ID: {productId})"
                    });
                }

                // Kiểm tra số lượng tồn kho
                if (product.SoLuongTonKho < quantity)
                    return Json(new { success = false, message = "Số lượng sản phẩm trong kho không đủ" });

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var cartItem = await _context.GioHangChiTiets
                    .FirstOrDefaultAsync(ci => ci.GioHangId == cart.Id && ci.SanPhamId == productId);

                if (cartItem != null)
                {
                    // Nếu đã có thì cộng thêm số lượng nhưng không vượt quá tồn kho
                    var newQuantity = cartItem.SoLuong + quantity;
                    if (newQuantity > product.SoLuongTonKho)
                        return Json(new { success = false, message = "Số lượng sản phẩm trong kho không đủ" });
                        
                    cartItem.SoLuong = newQuantity;
                }
                else
                {
                    // Nếu chưa có thì thêm mới
                    cartItem = new GioHangChiTiet
                    {
                        GioHangId = cart.Id,
                        SanPhamId = productId,
                        SoLuong = quantity
                    };
                    _context.GioHangChiTiets.Add(cartItem);
                }

                await _context.SaveChangesAsync();
                
                // Cập nhật số lượng sản phẩm trong giỏ hàng
                var cartItemCount = await _context.GioHangChiTiets
                    .Where(ci => ci.GioHangId == cart.Id)
                    .SumAsync(ci => ci.SoLuong);
                    
                // Cập nhật ViewData
                ViewData["CartItemCount"] = cartItemCount;

                return Json(new { 
                    success = true, 
                    message = "Đã thêm vào giỏ hàng thành công",
                    cartItemCount = cartItemCount
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                return Json(new { 
                    success = false, 
                    message = "Có lỗi xảy ra khi thêm vào giỏ hàng: " + ex.Message 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(int itemId, int quantity)
        {
            if (quantity < 1) return BadRequest("Số lượng không hợp lệ");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.GioHangChiTiets
                .Include(ci => ci.GioHang)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.GioHang.UserId == userId);

            if (cartItem == null) return NotFound("Sản phẩm không tồn tại trong giỏ hàng");

            cartItem.SoLuong = quantity;
            await _context.SaveChangesAsync();
            
            // Cập nhật số lượng sản phẩm trong giỏ hàng
            var cart = await _context.GioHangs
                .Include(c => c.GioHangChiTiets)
                .FirstOrDefaultAsync(c => c.Id == cartItem.GioHangId);
                
            if (cart != null)
            {
                ViewData["CartItemCount"] = cart.GioHangChiTiets.Sum(ci => ci.SoLuong);
            }

            return Json(new { 
                success = true, 
                message = "Cập nhật giỏ hàng thành công",
                cartItemCount = ViewData["CartItemCount"]
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int itemId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.GioHangChiTiets
                .Include(ci => ci.GioHang)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.GioHang.UserId == userId);

            if (cartItem != null)
            {
                _context.GioHangChiTiets.Remove(cartItem);
                await _context.SaveChangesAsync();
                
                // Cập nhật số lượng sản phẩm trong giỏ hàng
                var cart = await _context.GioHangs
                    .Include(c => c.GioHangChiTiets)
                    .FirstOrDefaultAsync(c => c.Id == cartItem.GioHangId);
                    
                var cartItemCount = cart?.GioHangChiTiets.Sum(ci => ci.SoLuong) ?? 0;
                ViewData["CartItemCount"] = cartItemCount;
                
                return Json(new { 
                    success = true, 
                    message = "Đã xóa sản phẩm khỏi giỏ hàng",
                    cartItemCount = cartItemCount
                });
            }

            return NotFound("Không tìm thấy sản phẩm trong giỏ hàng");
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { count = 0 });
                }

                var cart = await _context.GioHangs
                    .Include(c => c.GioHangChiTiets)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    return Json(new { count = 0 });
                }


                var totalItems = cart.GioHangChiTiets.Sum(item => item.SoLuong);
                return Json(new { count = totalItems });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                Console.WriteLine($"Lỗi khi lấy số lượng giỏ hàng: {ex.Message}");
                return Json(new { count = 0 });
            }
        }

        private async Task<GioHang> GetOrCreateCartAsync(string userId)
        {
            var cart = await _context.GioHangs
                .Include(c => c.GioHangChiTiets)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new GioHang { UserId = userId };
                _context.GioHangs.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Cập nhật ViewData với số lượng sản phẩm hiện tại
            UpdateCartItemCount(userId);
            
            return cart;
        }
        
        private void UpdateCartItemCount(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                ViewData["CartItemCount"] = 0;
                TempData["CartItemCount"] = 0;
                return;
            }

            var cart = _context.GioHangs
                .Include(c => c.GioHangChiTiets)
                .FirstOrDefault(c => c.UserId == userId);
                
            var count = cart?.GioHangChiTiets.Sum(ci => ci.SoLuong) ?? 0;
            
            // Cập nhật tất cả các nơi lưu trữ số lượng
            ViewData["CartItemCount"] = count;
            TempData["CartItemCount"] = count;
            
            // Lưu vào HttpContext.Items để sử dụng trong các filter khác
            if (HttpContext != null)
            {
                HttpContext.Items["CartItemCount"] = count;
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Người dùng chưa đăng nhập" });
                }
                
                var cart = await _context.GioHangs
                    .Include(c => c.GioHangChiTiets)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
                    
                if (cart == null || !cart.GioHangChiTiets.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng đã trống" });
                }
                
                // Xóa tất cả chi tiết giỏ hàng
                _context.GioHangChiTiets.RemoveRange(cart.GioHangChiTiets);
                await _context.SaveChangesAsync();
                
                // Cập nhật số lượng sản phẩm
                UpdateCartItemCount(userId);
                
                return Json(new { 
                    success = true, 
                    message = "Đã xóa tất cả sản phẩm khỏi giỏ hàng",
                    cartItemCount = 0
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Có lỗi xảy ra khi xóa giỏ hàng: " + ex.Message 
                });
            }
        }
    }
}
