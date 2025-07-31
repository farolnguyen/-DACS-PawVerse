using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PawVerse.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.SqlServer;
using System.Linq;
namespace PawVerse.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<DanhSachYeuThich> DanhSachYeuThiches { get; set; }
        public DbSet<DonHang> DonHangs { get; set; }
        public DbSet<LichSuMuaHang> LichSuMuaHangs { get; set; }
        public DbSet<NhaCungCap> NhaCungCaps { get; set; }
        public DbSet<PhanQuyen> PhanQuyens { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<ThuongHieu> ThuongHieus { get; set; }
        public DbSet<VanChuyen> VanChuyens { get; set; }
        public DbSet<GioHang> GioHangs { get; set; }
        public DbSet<GioHangChiTiet> GioHangChiTiets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=FAROL-PC\\SQLEXPRESS;Database=PawVerse;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình GioHang
            modelBuilder.Entity<GioHang>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình GioHangChiTiet
            modelBuilder.Entity<GioHangChiTiet>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.GioHang)
                    .WithMany(g => g.GioHangChiTiets)
                    .HasForeignKey(e => e.GioHangId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.SanPham)
                    .WithMany()
                    .HasForeignKey(e => e.SanPhamId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.SoLuong)
                    .IsRequired()
                    .HasDefaultValue(1);
            });

            // Cấu hình ApplicationUser
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("AspNetUsers");
                entity.HasKey(e => e.Id);
                
                // Cấu hình quan hệ với PhanQuyen
                entity.HasOne(u => u.PhanQuyen)
                      .WithMany()
                      .HasForeignKey(u => u.IdPhanQuyen)
                      .OnDelete(DeleteBehavior.SetNull);

                // Cấu hình quan hệ với DanhSachYeuThich
                entity.HasMany(u => u.DanhSachYeuThiches)
                      .WithOne(d => d.IdNguoiDungNavigation)
                      .HasForeignKey(d => d.IdNguoiDung)
                      .OnDelete(DeleteBehavior.Cascade);

                // Cấu hình quan hệ với DonHang
                entity.HasMany(u => u.DonHangs)
                      .WithOne(d => d.NguoiDung)
                      .HasForeignKey(d => d.IdNguoiDung)
                      .OnDelete(DeleteBehavior.Cascade);

                // Cấu hình quan hệ với LichSuMuaHang
                entity.HasMany(u => u.LichSuMuaHangs)
                      .WithOne(l => l.IdNguoiDungNavigation)
                      .HasForeignKey(l => l.IdNguoiDung)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình DanhSachYeuThich
            modelBuilder.Entity<DanhSachYeuThich>(entity =>
            {
                entity.HasOne(d => d.IdSanPhamNavigation)
                      .WithMany()
                      .HasForeignKey(d => d.IdSanPham)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình DonHang
            modelBuilder.Entity<DonHang>(entity =>
            {
                entity.HasOne(d => d.IdCouponNavigation)
                      .WithMany()
                      .HasForeignKey(d => d.IdCoupon)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.VanChuyen)
                      .WithMany()
                      .HasForeignKey(d => d.IdVanChuyen)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Cấu hình LichSuMuaHang
            modelBuilder.Entity<LichSuMuaHang>(entity =>
            {
                entity.HasOne(l => l.IdSanPhamNavigation)
                      .WithMany()
                      .HasForeignKey(l => l.IdSanPham)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable("AspNetRoles");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("AspNetUserRoles");
                entity.HasKey(e => new { e.UserId, e.RoleId });
            });

            modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("AspNetUserClaims");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("AspNetUserLogins");
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
            });

            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("AspNetUserTokens");
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
            });

            modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("AspNetRoleClaims");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<ChiTietDonHang>(entity =>
            {
                entity.HasKey(e => e.IdChiTietDonHang).HasName("PK__ChiTietD__2B84021AB271F01E");

                entity.HasOne(d => d.IdDonHangNavigation).WithMany(p => p.ChiTietDonHangs)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ChiTietDo__ID_Do__71D1E811");

                entity.HasOne(d => d.SanPham).WithMany(p => p.ChiTietDonHangs)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ChiTietDo__ID_Sa__72C60C4A");
            });

            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.HasKey(e => e.IdCoupon).HasName("PK__Coupon__BB3EF106E11088BE");

                entity.Property(e => e.TrangThai).HasDefaultValue("Hoạt động");
            });

            modelBuilder.Entity<DanhMuc>(entity =>
            {
                entity.HasKey(e => e.IdDanhMuc).HasName("PK__DanhMuc__662ACB01C03355F0");

                entity.Property(e => e.TrangThai).HasDefaultValue("Đang bán");
            });

            modelBuilder.Entity<DanhSachYeuThich>(entity =>
            {
                entity.HasKey(e => e.IdYeuThich).HasName("PK__DanhSach__F37790DE2D983BEB");

                entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.NgayThem).HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.IdNguoiDungNavigation).WithMany(p => p.DanhSachYeuThiches)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DanhSachY__ID_Ng__778AC167");

                entity.HasOne(d => d.IdSanPhamNavigation).WithMany(p => p.DanhSachYeuThiches)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DanhSachY__ID_Sa__787EE5A0");
            });

            modelBuilder.Entity<DonHang>(entity =>
            {
                entity.HasKey(e => e.IdDonHang).HasName("PK__DonHang__99B726395D42AA64");

                entity.Property(e => e.NgayDatHang).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.TrangThai).HasDefaultValue("Chờ xử lý");

                entity.HasOne(d => d.IdCouponNavigation).WithMany(p => p.DonHangs).HasConstraintName("FK__DonHang__ID_Coup__6E01572D");

                entity.HasOne(d => d.NguoiDung).WithMany(p => p.DonHangs)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DonHang__ID_Nguo__6D0D32F4");

                entity.HasOne(d => d.VanChuyen).WithMany(p => p.DonHangs)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DonHang__ID_VanC__6EF57B66");
            });

            modelBuilder.Entity<LichSuMuaHang>(entity =>
            {
                entity.HasKey(e => e.IdLichSu).HasName("PK__LichSuMu__156319B5C9A61419");

                entity.Property(e => e.NgayMua).HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.IdSanPhamNavigation).WithMany()
                    .HasForeignKey(d => d.IdSanPham)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__LichSuMua__ID_Sa__7D439ABD");
            });

            modelBuilder.Entity<NhaCungCap>(entity =>
            {
                entity.HasKey(e => e.IdNhaCungCap).HasName("PK__NhaCungC__D1E6E45E02447FD4");

                entity.Property(e => e.TrangThai).HasDefaultValue("Hoạt động");
            });

            modelBuilder.Entity<PhanQuyen>(entity =>
            {
                entity.HasKey(e => e.IdPhanQuyen).HasName("PK__PhanQuye__639A42B6B311B94B");
            });

            modelBuilder.Entity<SanPham>(entity =>
            {
                entity.HasKey(e => e.IdSanPham).HasName("PK__SanPham__617EA392A9EA29D6");

                entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.NgayTao).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.TrangThai).HasDefaultValue("Còn hàng");

                // Original relationship
                entity.HasOne(d => d.IdDanhMucNavigation)
                    .WithMany(p => p.SanPhams)
                    .HasForeignKey(d => d.IdDanhMuc)
                    .HasConstraintName("FK__SanPham__ID_Danh__5812160E")
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.IdThuongHieuNavigation)
                    .WithMany(p => p.SanPhams)
                    .HasForeignKey(d => d.IdThuongHieu)
                    .HasConstraintName("FK__SanPham__ID_Thuo__59063A47")
                    .OnDelete(DeleteBehavior.ClientSetNull);

                // Map properties to actual database columns
                entity.Property(e => e.IdDanhMuc).HasColumnName("IdDanhMuc");
                entity.Property(e => e.IdThuongHieu).HasColumnName("IdThuongHieu");
                entity.Property(e => e.IdDanhMucNavigationIdDanhMuc).HasColumnName("IdDanhMucNavigationIdDanhMuc");
                entity.Property(e => e.IdThuongHieuNavigationIdThuongHieu).HasColumnName("IdThuongHieuNavigationIdThuongHieu");

                // Additional relationship for the extra foreign key columns
                entity.HasOne(d => d.IdDanhMucNavigationIdDanhMucNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.IdDanhMucNavigationIdDanhMuc)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SanPham_DanhMuc_Extra");

                entity.HasOne(d => d.IdThuongHieuNavigationIdThuongHieuNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.IdThuongHieuNavigationIdThuongHieu)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SanPham_ThuongHieu_Extra");
            });

            modelBuilder.Entity<ThuongHieu>(entity =>
            {
                entity.HasKey(e => e.IdThuongHieu).HasName("PK__ThuongHi__AB2A011AE4E220AB");

                entity.Property(e => e.TrangThai).HasDefaultValue("Hoạt động");
            });

            modelBuilder.Entity<VanChuyen>(entity =>
            {
                entity.HasKey(e => e.IdVanChuyen).HasName("PK__VanChuye__626CD04B88CEB2AF");
            });
        }
    }
}
