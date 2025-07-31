using System.ComponentModel.DataAnnotations;
using PawVerse.Models.Location;

namespace PawVerse.Models.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        // Address information
        public AddressSelectionModel AddressInfo { get; set; } = new();

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; }

        [Display(Name = "Phương thức vận chuyển")]
        [Required(ErrorMessage = "Vui lòng chọn phương thức vận chuyển")]
        public int SelectedVanChuyenId { get; set; }

        public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableVanChuyenOptions { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

        // Thông tin giỏ hàng
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total { get; set; }
    }

    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
