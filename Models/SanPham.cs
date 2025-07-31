using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PawVerse.Models;

public partial class SanPham
{
    [Key]
    [Column("IdSanPham")]
    public int IdSanPham { get; set; }


    [Required]
    [StringLength(255)]
    [Column("TenSanPham")]
    public string TenSanPham { get; set; } = null!;

    [Required]
    [StringLength(255)]
    [Column("TenAlias")]
    public string TenAlias { get; set; } = null!;

    [Column("IdDanhMuc")]
    public int IdDanhMuc { get; set; }


    [Column("IdThuongHieu")]
    public int IdThuongHieu { get; set; }


    [Required]
    [StringLength(50)]
    [Column("TrongLuong")]
    public string TrongLuong { get; set; } = null!;


    [Column("MauSac")]
    [StringLength(50)]
    public string? MauSac { get; set; }


    [Column("XuatXu")]
    [StringLength(100)]
    public string? XuatXu { get; set; }


    [Column("MoTa")]
    public string? MoTa { get; set; }


    [Column("SoLuongTonKho")]
    public int SoLuongTonKho { get; set; }


    [Column("SoLuongDaBan")]
    public int SoLuongDaBan { get; set; }


    [Column("GiaBan", TypeName = "decimal(18,2)")]
    public decimal GiaBan { get; set; }


    [Column("GiaVon", TypeName = "decimal(18,2)")]
    public decimal GiaVon { get; set; }


    [Column("GiaKhuyenMai", TypeName = "decimal(18,2)")]
    public decimal? GiaKhuyenMai { get; set; }


    [Column("HinhAnh")]
    [StringLength(255)]
    public string? HinhAnh { get; set; }


    [Column("NgaySanXuat")]
    public DateTime NgaySanXuat { get; set; }


    [Column("HanSuDung")]
    public DateTime HanSuDung { get; set; }


    [Column("TrangThai")]
    [StringLength(50)]
    public string TrangThai { get; set; } = "Còn hàng";


    [Column("SoLanXem")]
    public int SoLanXem { get; set; }


    [Column("NgayTao")]
    public DateTime NgayTao { get; set; } = DateTime.Now;


    [Column("NgayCapNhat")]
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;


    // Navigation properties
    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();
    public virtual ICollection<DanhSachYeuThich> DanhSachYeuThiches { get; set; } = new List<DanhSachYeuThich>();
    public virtual ICollection<LichSuMuaHang> LichSuMuaHangs { get; set; } = new List<LichSuMuaHang>();

    // Navigation properties for the main foreign keys
    [JsonIgnore]
    [ForeignKey("IdDanhMuc")]
    public virtual DanhMuc? IdDanhMucNavigation { get; set; }

    [JsonIgnore]
    [ForeignKey("IdThuongHieu")]
    public virtual ThuongHieu? IdThuongHieuNavigation { get; set; }
    
    // Additional foreign key columns
    [Column("IdDanhMucNavigationIdDanhMuc")]
    public int IdDanhMucNavigationIdDanhMuc { get; set; }
    
    [Column("IdThuongHieuNavigationIdThuongHieu")]
    public int IdThuongHieuNavigationIdThuongHieu { get; set; }
    
    // Navigation properties for the additional foreign key columns
    [JsonIgnore]
    [ForeignKey("IdDanhMucNavigationIdDanhMuc")]
    public virtual DanhMuc? IdDanhMucNavigationIdDanhMucNavigation { get; set; }
    
    [JsonIgnore]
    [ForeignKey("IdThuongHieuNavigationIdThuongHieu")]
    public virtual ThuongHieu? IdThuongHieuNavigationIdThuongHieuNavigation { get; set; }
}
