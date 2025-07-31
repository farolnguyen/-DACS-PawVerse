using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using PawVerse.Models;

namespace PawVerse.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để sử dụng chức năng này
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> ToggleWishlist([FromBody] ToggleWishlistRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để thêm vào yêu thích" });
                }

                // Kiểm tra sản phẩm có tồn tại không
                var product = await _context.SanPhams.FindAsync(request.ProductId);
                if (product == null)
                {
                    return NotFound(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // Kiểm tra xem sản phẩm đã có trong danh sách yêu thích chưa
                var existingWishlistItem = await _context.DanhSachYeuThiches
                    .FirstOrDefaultAsync(w => w.IdNguoiDung == userId && w.IdSanPham == request.ProductId);

                bool isAdded = false;

                if (existingWishlistItem != null)
                {
                    // Nếu đã tồn tại thì xóa khỏi danh sách yêu thích
                    _context.DanhSachYeuThiches.Remove(existingWishlistItem);
                    isAdded = false;
                }
                else
                {
                    // Nếu chưa tồn tại thì thêm mới vào danh sách yêu thích
                    var wishlistItem = new DanhSachYeuThich
                    {
                        IdNguoiDung = userId,
                        IdSanPham = request.ProductId,
                        NgayThem = DateTime.Now,
                        NgayCapNhat = DateTime.Now
                    };
                    _context.DanhSachYeuThiches.Add(wishlistItem);
                    isAdded = true;
                }

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true, 
                    isAdded = isAdded,
                    message = isAdded ? "Đã thêm vào danh sách yêu thích" : "Đã xóa khỏi danh sách yêu thích"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckWishlistStatus([FromQuery] int productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return BadRequest(new { success = false, message = "ID sản phẩm không hợp lệ" });
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, isInWishlist = false });
                }

                var isInWishlist = await _context.DanhSachYeuThiches
                    .AnyAsync(w => w.IdNguoiDung == userId && w.IdSanPham == productId);

                return Ok(new { success = true, isInWishlist });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }

    public class ToggleWishlistRequest
    {
        public int ProductId { get; set; }
    }
}
