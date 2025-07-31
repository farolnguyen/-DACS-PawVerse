using System;
using System.Collections.Generic;

namespace PawVerse.Models;

public partial class ChiTietDonHang
{
    public int IdChiTietDonHang { get; set; }

    public int IdDonHang { get; set; }

    public int IdSanPham { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public virtual DonHang IdDonHangNavigation { get; set; } = null!;

    public virtual SanPham SanPham { get; set; } = null!;
}
