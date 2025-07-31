using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PawVerse.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Địa chỉ")]
        public string? DiaChi { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Giới tính")]
        public string? GioiTinh { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        [Display(Name = "Ảnh đại diện")]
        public string? Avatar { get; set; }

        // Navigation properties
        public virtual ICollection<DanhSachYeuThich> DanhSachYeuThiches { get; set; } = new List<DanhSachYeuThich>();
        public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
        public virtual ICollection<LichSuMuaHang> LichSuMuaHangs { get; set; } = new List<LichSuMuaHang>();
        
        [ForeignKey("IdPhanQuyen")]
        public virtual PhanQuyen? PhanQuyen { get; set; }
        public int? IdPhanQuyen { get; set; }
    }
}
