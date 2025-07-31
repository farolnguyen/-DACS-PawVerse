using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PawVerse.Models;

public partial class DanhSachYeuThich
{
    public int IdYeuThich { get; set; }

    public string IdNguoiDung { get; set; }

    public int IdSanPham { get; set; }

    public DateTime NgayThem { get; set; } = DateTime.Now;

    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    [ForeignKey("IdNguoiDung")]
    public virtual ApplicationUser IdNguoiDungNavigation { get; set; } = null!;

    [ForeignKey("IdSanPham")]
    public virtual SanPham IdSanPhamNavigation { get; set; } = null!;
}
