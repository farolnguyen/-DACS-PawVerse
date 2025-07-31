using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PawVerse.Models;

public partial class ThuongHieu
{
    public int IdThuongHieu { get; set; }

    [Required(ErrorMessage = "Tên thương hiệu là bắt buộc")]
    [Display(Name = "Tên thương hiệu")]
    public string TenThuongHieu { get; set; } = null!;

    [Display(Name = "Tên rút gọn")]
    public string? TenAlias { get; set; }

    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Display(Name = "Logo")]
    public string? Logo { get; set; }

    [Display(Name = "Trạng thái")]
    public string? TrangThai { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
