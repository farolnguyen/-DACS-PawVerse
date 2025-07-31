using System;
using System.Collections.Generic;

namespace PawVerse.Models;

public partial class VanChuyen
{
    public int IdVanChuyen { get; set; }

    public string TenVanChuyen { get; set; } = null!;

    public string Tinh { get; set; } = null!;

    public string QuanHuyen { get; set; } = null!;

    public string PhuongXa { get; set; } = null!;

    public string Duong { get; set; } = null!;

    public decimal PhiVanChuyen { get; set; }

    public string ThoiGianGiaoHang { get; set; } = null!;

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
