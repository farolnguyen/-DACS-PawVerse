using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PawVerse.Models;

public partial class LichSuMuaHang
{
    public int IdLichSu { get; set; }

    public string IdNguoiDung { get; set; }

    public int IdSanPham { get; set; }

    public int SoLuong { get; set; }

    public DateTime NgayMua { get; set; } = DateTime.Now;

    public decimal TongTien { get; set; }

    [ForeignKey("IdNguoiDung")]
    public virtual ApplicationUser IdNguoiDungNavigation { get; set; } = null!;

    [ForeignKey("IdSanPham")]
    public virtual SanPham IdSanPhamNavigation { get; set; } = null!;
}
