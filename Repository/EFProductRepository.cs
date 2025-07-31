using Microsoft.EntityFrameworkCore;
using PawVerse.Data;
using PawVerse.Models;

public class EFProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _db;
    public EFProductRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<SanPham>> GetAllAsync()
        => await _db.SanPhams.AsNoTracking().ToListAsync();

    public async Task<SanPham?> GetByIdAsync(int id)
        => await _db.SanPhams.FindAsync(id);

    public async Task AddAsync(SanPham product)
    {
        _db.SanPhams.Add(product);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SanPham product)
    {
        _db.SanPhams.Update(product);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.SanPhams.FindAsync(id);
        if (entity is null) return;
        _db.SanPhams.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
