using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using PawVerse.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace PawVerse.Filters
{
    public class CartActionFilter : IActionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartActionFilter(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var cart = _context.GioHangs
                    .Include(c => c.GioHangChiTiets)
                    .FirstOrDefault(c => c.UserId == userId);

                if (cart != null)
                {
                    var count = cart.GioHangChiTiets.Sum(ci => ci.SoLuong);
                    context.HttpContext.Items["CartItemCount"] = count;
                    
                    // Cập nhật vào ViewData và HttpContext.Items
                    if (context.Controller is Controller controller)
                    {
                        // Đặt vào ViewData
                        controller.ViewData["CartItemCount"] = count;
                        
                        // Đặt vào HttpContext.Items để sử dụng trong các request tiếp theo
                        controller.HttpContext.Items["CartItemCount"] = count;
                        
                        // Đặt vào TempData để giữ giá trị qua redirect
                        controller.TempData["CartItemCount"] = count;
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
