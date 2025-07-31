using System;
using System.Collections.Generic;
using PawVerse.Models;

namespace PawVerse.Areas.Admin.ViewModels
{
    public class OrderListViewModel
    {
        public int IdDonHang { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public DateTime NgayDatHang { get; set; }
        public string TrangThai { get; set; }
        public decimal TongTien { get; set; }
        public string PhuongThucThanhToan { get; set; }
    }

    public class OrderDetailViewModel
    {
        public int IdDonHang { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoai { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public DateTime NgayDatHang { get; set; }
        public DateTime? NgayGiaoHangDuKien { get; set; }
        public DateTime? NgayHuy { get; set; }
        public string TrangThai { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public decimal TongTien { get; set; }
        public string? CouponCode { get; set; }
        public decimal? GiamGia { get; set; }
        public decimal ThanhTien { get; set; }
        public List<OrderItemViewModel> ChiTietDonHangs { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }

    public class UpdateOrderStatusViewModel
    {
        public int IdDonHang { get; set; }
        public string TrangThai { get; set; }
        public string? GhiChu { get; set; }
    }
}
