using System;
using System.Collections.Generic;

namespace PawVerse.Models;

public partial class PhanQuyen
{
    public int IdPhanQuyen { get; set; }

    public string TenPhanQuyen { get; set; } = null!;

    public string TenAlias { get; set; } = null!;

    public string Quyen { get; set; } = null!;

    // Đã xóa tham chiếu đến NguoiDung vì đã chuyển sang sử dụng ApplicationUser
}
