using System.Collections.Generic;
using System.Threading.Tasks;
using PawVerse.Models;        // nơi chứa class SanPham

public interface IProductRepository
{
    // Lấy toàn bộ sản phẩm
    Task<IEnumerable<SanPham>> GetAllAsync();

    // Lấy 1 sản phẩm theo ID
    Task<SanPham?> GetByIdAsync(int id);

    // Thêm mới
    Task AddAsync(SanPham product);

    // Cập nhật
    Task UpdateAsync(SanPham product);

    // Xóa
    Task DeleteAsync(int id);
}
