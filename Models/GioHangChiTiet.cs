using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PawVerse.Models
{
    public class GioHangChiTiet
    {
        [Key]
        public int Id { get; set; }
        
        public int GioHangId { get; set; }
        
        [ForeignKey("GioHangId")]
        public virtual GioHang GioHang { get; set; }
        
        public int SanPhamId { get; set; }
        
        [ForeignKey("SanPhamId")]
        public virtual SanPham SanPham { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; } = 1;
        
        [NotMapped]
        public decimal ThanhTien => SanPham?.GiaKhuyenMai > 0 ? 
                                 SanPham.GiaKhuyenMai.Value * SoLuong : 
                                 SanPham?.GiaBan * SoLuong ?? 0;
    }
}
