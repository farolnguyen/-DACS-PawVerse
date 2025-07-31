using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PawVerse.Areas.Admin.Models
{
    public class RoleManagementViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Phân quyền hiện tại")]
        public string CurrentRole { get; set; } = "Chưa có phân quyền";
        
        [Display(Name = "Chọn phân quyền mới")]
        public string? SelectedRole { get; set; }
        
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
    }
}
