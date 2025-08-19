using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using PawVerse.Models;
using PawVerse.Areas.Admin.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace PawVerse.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<AccountController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Admin/Account
        public async Task<IActionResult> Index()
        {
            // Lấy thông tin người dùng hiện tại
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;
            
            var users = await _userManager.Users
                .Include(u => u.PhanQuyen)
                .Where(u => u.Id != currentUserId) // Loại bỏ tài khoản đang đăng nhập
                .OrderBy(u => u.FullName)
                .Select(u => new UserListViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName ?? string.Empty,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    EmailConfirmed = u.EmailConfirmed,
                    LockoutEnabled = u.LockoutEnabled,
                    LockoutEnd = u.LockoutEnd,
                    NgayTao = u.NgayTao,
                    Avatar = u.Avatar,
                    PhanQuyen = u.PhanQuyen
                })
                .ToListAsync();
                
            var userRoles = new Dictionary<string, IList<string>>();
            
            foreach (var user in users)
            {
                var appUser = await _userManager.FindByIdAsync(user.Id);
                var roles = await _userManager.GetRolesAsync(appUser);
                userRoles[user.Id] = roles;
            }
            
            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // GET: Admin/Account/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.Users
                .Include(u => u.PhanQuyen)
                .FirstOrDefaultAsync(u => u.Id == id);
                
            if (user == null)
            {
                return NotFound();
            }
            
            var userRoles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRoles = userRoles;
            ViewData["CurrentRole"] = userRoles.FirstOrDefault() ?? "Chưa có phân quyền";
            
            // Get user's orders
            var orders = await _context.DonHangs
                .Where(o => o.IdNguoiDung == id)
                .OrderByDescending(o => o.NgayDatHang)
                .Take(5)
                .ToListAsync();
                
            ViewBag.Orders = orders;
            ViewBag.OrdersCount = await _context.DonHangs.CountAsync(o => o.IdNguoiDung == id);

            return View(user);
        }

        // GET: Admin/Account/ChangePassword/5
        public async Task<IActionResult> ChangePassword(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty
            };

            return View(model);
        }

        // POST: Admin/Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Index));
                }

                // Generate a password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Reset the password
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                
                if (result.Succeeded)
                {
                    // Update security stamp to invalidate any existing tokens
                    await _userManager.UpdateSecurityStampAsync(user);
                    
                    TempData["SuccessMessage"] = "Đã đặt lại mật khẩu thành công.";
                    return RedirectToAction(nameof(Index));
                }
                
                // If we got this far, something failed
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                
                // Repopulate the model to show the form again with errors
                model.Email = user.Email ?? string.Empty;
                model.UserName = user.UserName ?? string.Empty;
                return View(model);
            }
            catch (Exception ex)
            {
                // Log the exception (you should implement proper logging)
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại.");
                return View(model);
            }
        }

        // POST: Admin/Account/LockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Lock the user for 1000 days
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(1000));
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đã khóa tài khoản thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi khóa tài khoản.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Account/UnlockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Unlock the user
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đã mở khóa tài khoản thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi mở khóa tài khoản.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        // GET: Admin/Account/ManageRoles/5
        [HttpGet]
        public async Task<IActionResult> ManageRoles(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.ToList();

            var model = new RoleManagementViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                CurrentRole = userRoles.FirstOrDefault() ?? "Chưa có phân quyền",
                AvailableRoles = allRoles.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name
                }).ToList()
            };

            return View(model);
        }

        // POST: Admin/Account/ManageRoles/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(RoleManagementViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate the roles dropdown if the model is invalid
                model.AvailableRoles = _roleManager.Roles
                    .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Name
                    }).ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Remove user from all current roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                // Add user to the selected role if one was selected
                if (!string.IsNullOrEmpty(model.SelectedRole))
                {
                    await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    
                    // Update user's PhanQuyen if needed
                    var phanQuyen = await _context.PhanQuyens
                        .FirstOrDefaultAsync(pq => pq.TenPhanQuyen == model.SelectedRole);
                        
                    if (phanQuyen != null)
                    {
                        user.IdPhanQuyen = phanQuyen.IdPhanQuyen;
                        await _userManager.UpdateAsync(user);
                    }
                }
                else
                {
                    // If no role is selected, clear the PhanQuyen
                    user.IdPhanQuyen = null;
                    await _userManager.UpdateAsync(user);
                }

                TempData["SuccessMessage"] = "Cập nhật phân quyền thành công!";
                return RedirectToAction(nameof(Details), new { id = model.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật phân quyền cho người dùng {UserId}", model.UserId);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật phân quyền. Vui lòng thử lại.");
                
                // Repopulate the roles dropdown
                model.AvailableRoles = _roleManager.Roles
                    .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Name
                    }).ToList();
                    
                return View(model);
            }
        }
    }
}
//Tmd1eeG7hW4gSOG6o2kgxJDEg25nIC0gQsO5aSBC4bqjbyBIw6JuIA==