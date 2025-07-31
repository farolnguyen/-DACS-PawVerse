using System;
using PawVerse.Models;

namespace PawVerse.Areas.Admin.Models
{
    public class UserListViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public DateTime NgayTao { get; set; }
        public string Avatar { get; set; }
        public PhanQuyen PhanQuyen { get; set; }
    }
}
