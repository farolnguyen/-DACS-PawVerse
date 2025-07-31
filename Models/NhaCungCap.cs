using System;
using System.Collections.Generic;

namespace PawVerse.Models;

public partial class NhaCungCap
{
    public int IdNhaCungCap { get; set; }

    public string TenNhaCungCap { get; set; } = null!;

    public string LogoNcc { get; set; } = null!;

    public string? DiaChi { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public string? MoTa { get; set; }

    public string? NguoiLienLac { get; set; }

    public string TrangThai { get; set; } = null!;
}
