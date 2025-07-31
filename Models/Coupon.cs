using System;
using System.Collections.Generic;

namespace PawVerse.Models;

public partial class Coupon
{
    public int IdCoupon { get; set; }

    public string TenMaCoupon { get; set; } = null!;

    public string? MoTa { get; set; }

    public DateOnly NgayBatDau { get; set; }

    public DateOnly NgayKetThuc { get; set; }

    public decimal MucGiamGia { get; set; }

    public string LoaiGiamGia { get; set; } = null!;

    public int SoLuong { get; set; }

    public string TrangThai { get; set; } = null!;

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
