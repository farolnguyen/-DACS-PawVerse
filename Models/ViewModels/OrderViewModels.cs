using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PawVerse.Models.ViewModels
{
    /// <summary>
    /// ViewModel for displaying order confirmation details
    /// </summary>
    public class OrderConfirmationViewModel
    {
        public int IdDonHang { get; set; }
        public DateTime NgayDatHang { get; set; }
        public string? TenKhachHang { get; set; }
        public string? SoDienThoai { get; set; }
        public string? DiaChiGiaoHang { get; set; }
        public string? TenVanChuyen { get; set; }
        public string? ThoiGianGiaoHangUocTinh { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public string? PhuongThucThanhToan { get; set; }
        public decimal TongTienDonHang { get; set; }
        public string? GhiChu { get; set; }
        public string? TrangThaiDonHang { get; set; }
        public List<OrderItemViewModel> ChiTietDonHangs { get; set; } = new List<OrderItemViewModel>();
    }

    public class OrderHistoryViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string PaymentMethod { get; set; }
        public string Note { get; set; }
        public bool CanCancel { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
    }

    public class OrderItemViewModel
    {
        public int SanPhamId { get; set; }
        public string? TenSanPham { get; set; }
        public string? ImageUrl { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; } // Calculated in controller or by getter
    }

    public class UpdateOrderViewModel
    {
        public int OrderId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố")]
        public string SelectedProvinceCode { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn quận/huyện")]
        public string SelectedDistrictCode { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn phường/xã")]
        public string SelectedWardCode { get; set; }
        
        public string StreetAddress { get; set; }
        public string Note { get; set; }
    }
}
