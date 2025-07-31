using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PawVerse.Models;

public partial class DanhMuc
{
    public int IdDanhMuc { get; set; }

    [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
    [Display(Name = "Tên danh mục")]
    public string TenDanhMuc { get; set; } = null!;

    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Display(Name = "Hình ảnh")]
    public string? HinhAnh { get; set; }

    [Display(Name = "Trạng thái")]
    public string? TrangThai { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
