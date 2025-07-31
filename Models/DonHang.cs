using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PawVerse.Models;

public partial class DonHang
{
    public int IdDonHang { get; set; }

    public string IdNguoiDung { get; set; }

    public string TenKhachHang { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public DateTime NgayDatHang { get; set; }

    public DateTime? NgayGiaoHangDuKien { get; set; }

    public DateTime? NgayHuy { get; set; }

    public string DiaChiGiaoHang { get; set; } = null!;

    public int? IdCoupon { get; set; }

    public int IdVanChuyen { get; set; }

    public decimal PhiVanChuyen { get; set; }

    public decimal TongTien { get; set; }

    public string TrangThai { get; set; } = null!;

    public string PhuongThucThanhToan { get; set; } = null!;

    public string? GhiChu { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    [ForeignKey("IdCoupon")]
    public virtual Coupon? IdCouponNavigation { get; set; }

    [ForeignKey("IdNguoiDung")]
    public virtual ApplicationUser NguoiDung { get; set; } = null!;

    [ForeignKey("IdVanChuyen")]
    public virtual VanChuyen VanChuyen { get; set; } = null!;
}
