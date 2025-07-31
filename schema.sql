CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Coupons] (
    [IdCoupon] int NOT NULL IDENTITY,
    [TenMaCoupon] nvarchar(max) NOT NULL,
    [MoTa] nvarchar(max) NULL,
    [NgayBatDau] date NOT NULL,
    [NgayKetThuc] date NOT NULL,
    [MucGiamGia] decimal(18,2) NOT NULL,
    [LoaiGiamGia] nvarchar(max) NOT NULL,
    [SoLuong] int NOT NULL,
    [TrangThai] nvarchar(max) NOT NULL DEFAULT N'Hoạt động',
    CONSTRAINT [PK__Coupon__BB3EF106E11088BE] PRIMARY KEY ([IdCoupon])
);
GO


CREATE TABLE [DanhMucs] (
    [IdDanhMuc] int NOT NULL IDENTITY,
    [TenDanhMuc] nvarchar(max) NOT NULL,
    [MoTa] nvarchar(max) NULL,
    [HinhAnh] nvarchar(max) NULL,
    [TrangThai] nvarchar(max) NULL DEFAULT N'Đang bán',
    CONSTRAINT [PK__DanhMuc__662ACB01C03355F0] PRIMARY KEY ([IdDanhMuc])
);
GO


CREATE TABLE [NhaCungCaps] (
    [IdNhaCungCap] int NOT NULL IDENTITY,
    [TenNhaCungCap] nvarchar(max) NOT NULL,
    [LogoNcc] nvarchar(max) NOT NULL,
    [DiaChi] nvarchar(max) NULL,
    [SoDienThoai] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [MoTa] nvarchar(max) NULL,
    [NguoiLienLac] nvarchar(max) NULL,
    [TrangThai] nvarchar(max) NOT NULL DEFAULT N'Hoạt động',
    CONSTRAINT [PK__NhaCungC__D1E6E45E02447FD4] PRIMARY KEY ([IdNhaCungCap])
);
GO


CREATE TABLE [PhanQuyens] (
    [IdPhanQuyen] int NOT NULL IDENTITY,
    [TenPhanQuyen] nvarchar(max) NOT NULL,
    [TenAlias] nvarchar(max) NOT NULL,
    [Quyen] nvarchar(max) NOT NULL,
    CONSTRAINT [PK__PhanQuye__639A42B6B311B94B] PRIMARY KEY ([IdPhanQuyen])
);
GO


CREATE TABLE [ThuongHieus] (
    [IdThuongHieu] int NOT NULL IDENTITY,
    [TenThuongHieu] nvarchar(max) NOT NULL,
    [TenAlias] nvarchar(max) NULL,
    [MoTa] nvarchar(max) NULL,
    [Logo] nvarchar(max) NULL,
    [TrangThai] nvarchar(max) NULL DEFAULT N'Hoạt động',
    CONSTRAINT [PK__ThuongHi__AB2A011AE4E220AB] PRIMARY KEY ([IdThuongHieu])
);
GO


CREATE TABLE [VanChuyens] (
    [IdVanChuyen] int NOT NULL IDENTITY,
    [TenVanChuyen] nvarchar(max) NOT NULL,
    [Tinh] nvarchar(max) NOT NULL,
    [QuanHuyen] nvarchar(max) NOT NULL,
    [PhuongXa] nvarchar(max) NOT NULL,
    [Duong] nvarchar(max) NOT NULL,
    [PhiVanChuyen] decimal(18,2) NOT NULL,
    [ThoiGianGiaoHang] nvarchar(max) NOT NULL,
    CONSTRAINT [PK__VanChuye__626CD04B88CEB2AF] PRIMARY KEY ([IdVanChuyen])
);
GO


CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [DiaChi] nvarchar(max) NULL,
    [NgaySinh] datetime2 NULL,
    [GioiTinh] nvarchar(max) NULL,
    [NgayTao] datetime2 NOT NULL,
    [NgayCapNhat] datetime2 NOT NULL,
    [Avatar] nvarchar(max) NULL,
    [IdPhanQuyen] int NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUsers_PhanQuyens_IdPhanQuyen] FOREIGN KEY ([IdPhanQuyen]) REFERENCES [PhanQuyens] ([IdPhanQuyen]) ON DELETE SET NULL
);
GO


CREATE TABLE [SanPhams] (
    [IdSanPham] int NOT NULL IDENTITY,
    [TenSanPham] nvarchar(255) NOT NULL,
    [TenAlias] nvarchar(255) NOT NULL,
    [IdDanhMuc] int NOT NULL,
    [IdThuongHieu] int NOT NULL,
    [TrongLuong] nvarchar(50) NOT NULL,
    [MauSac] nvarchar(50) NULL,
    [XuatXu] nvarchar(100) NULL,
    [MoTa] nvarchar(max) NULL,
    [SoLuongTonKho] int NOT NULL,
    [SoLuongDaBan] int NOT NULL,
    [GiaBan] decimal(18,2) NOT NULL,
    [GiaVon] decimal(18,2) NOT NULL,
    [GiaKhuyenMai] decimal(18,2) NULL,
    [HinhAnh] nvarchar(255) NULL,
    [NgaySanXuat] datetime2 NOT NULL,
    [HanSuDung] datetime2 NOT NULL,
    [TrangThai] nvarchar(50) NOT NULL DEFAULT N'Còn hàng',
    [SoLanXem] int NOT NULL,
    [NgayTao] datetime2 NOT NULL DEFAULT ((getdate())),
    [NgayCapNhat] datetime2 NOT NULL DEFAULT ((getdate())),
    [IdDanhMucNavigationIdDanhMuc] int NOT NULL,
    [IdThuongHieuNavigationIdThuongHieu] int NOT NULL,
    CONSTRAINT [PK__SanPham__617EA392A9EA29D6] PRIMARY KEY ([IdSanPham]),
    CONSTRAINT [FK_SanPham_DanhMuc_Extra] FOREIGN KEY ([IdDanhMucNavigationIdDanhMuc]) REFERENCES [DanhMucs] ([IdDanhMuc]),
    CONSTRAINT [FK_SanPham_ThuongHieu_Extra] FOREIGN KEY ([IdThuongHieuNavigationIdThuongHieu]) REFERENCES [ThuongHieus] ([IdThuongHieu]),
    CONSTRAINT [FK__SanPham__ID_Danh__5812160E] FOREIGN KEY ([IdDanhMuc]) REFERENCES [DanhMucs] ([IdDanhMuc]),
    CONSTRAINT [FK__SanPham__ID_Thuo__59063A47] FOREIGN KEY ([IdThuongHieu]) REFERENCES [ThuongHieus] ([IdThuongHieu])
);
GO


CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [DonHangs] (
    [IdDonHang] int NOT NULL IDENTITY,
    [IdNguoiDung] nvarchar(450) NOT NULL,
    [TenKhachHang] nvarchar(max) NOT NULL,
    [SoDienThoai] nvarchar(max) NOT NULL,
    [NgayDatHang] datetime2 NOT NULL DEFAULT ((getdate())),
    [NgayGiaoHangDuKien] datetime2 NULL,
    [NgayHuy] datetime2 NULL,
    [DiaChiGiaoHang] nvarchar(max) NOT NULL,
    [IdCoupon] int NULL,
    [IdVanChuyen] int NOT NULL,
    [TongTien] decimal(18,2) NOT NULL,
    [TrangThai] nvarchar(max) NOT NULL DEFAULT N'Chờ xử lý',
    [PhuongThucThanhToan] nvarchar(max) NOT NULL,
    CONSTRAINT [PK__DonHang__99B726395D42AA64] PRIMARY KEY ([IdDonHang]),
    CONSTRAINT [FK__DonHang__ID_Coup__6E01572D] FOREIGN KEY ([IdCoupon]) REFERENCES [Coupons] ([IdCoupon]) ON DELETE SET NULL,
    CONSTRAINT [FK__DonHang__ID_Nguo__6D0D32F4] FOREIGN KEY ([IdNguoiDung]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK__DonHang__ID_VanC__6EF57B66] FOREIGN KEY ([IdVanChuyen]) REFERENCES [VanChuyens] ([IdVanChuyen])
);
GO


CREATE TABLE [DanhSachYeuThiches] (
    [IdYeuThich] int NOT NULL IDENTITY,
    [IdNguoiDung] nvarchar(450) NOT NULL,
    [IdSanPham] int NOT NULL,
    [NgayThem] datetime2 NOT NULL DEFAULT ((getdate())),
    [NgayCapNhat] datetime2 NOT NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__DanhSach__F37790DE2D983BEB] PRIMARY KEY ([IdYeuThich]),
    CONSTRAINT [FK__DanhSachY__ID_Ng__778AC167] FOREIGN KEY ([IdNguoiDung]) REFERENCES [AspNetUsers] ([Id]),
    CONSTRAINT [FK__DanhSachY__ID_Sa__787EE5A0] FOREIGN KEY ([IdSanPham]) REFERENCES [SanPhams] ([IdSanPham])
);
GO


CREATE TABLE [LichSuMuaHangs] (
    [IdLichSu] int NOT NULL IDENTITY,
    [IdNguoiDung] nvarchar(450) NOT NULL,
    [IdSanPham] int NOT NULL,
    [SoLuong] int NOT NULL,
    [NgayMua] datetime2 NOT NULL DEFAULT ((getdate())),
    [TongTien] decimal(18,2) NOT NULL,
    [SanPhamIdSanPham] int NULL,
    CONSTRAINT [PK__LichSuMu__156319B5C9A61419] PRIMARY KEY ([IdLichSu]),
    CONSTRAINT [FK_LichSuMuaHangs_AspNetUsers_IdNguoiDung] FOREIGN KEY ([IdNguoiDung]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LichSuMuaHangs_SanPhams_SanPhamIdSanPham] FOREIGN KEY ([SanPhamIdSanPham]) REFERENCES [SanPhams] ([IdSanPham]),
    CONSTRAINT [FK__LichSuMua__ID_Sa__7D439ABD] FOREIGN KEY ([IdSanPham]) REFERENCES [SanPhams] ([IdSanPham]) ON DELETE CASCADE
);
GO


CREATE TABLE [ChiTietDonHangs] (
    [IdChiTietDonHang] int NOT NULL IDENTITY,
    [IdDonHang] int NOT NULL,
    [IdSanPham] int NOT NULL,
    [SoLuong] int NOT NULL,
    [DonGia] decimal(18,2) NOT NULL,
    [IdDonHangNavigationIdDonHang] int NOT NULL,
    [IdSanPhamNavigationIdSanPham] int NOT NULL,
    CONSTRAINT [PK__ChiTietD__2B84021AB271F01E] PRIMARY KEY ([IdChiTietDonHang]),
    CONSTRAINT [FK__ChiTietDo__ID_Do__71D1E811] FOREIGN KEY ([IdDonHangNavigationIdDonHang]) REFERENCES [DonHangs] ([IdDonHang]),
    CONSTRAINT [FK__ChiTietDo__ID_Sa__72C60C4A] FOREIGN KEY ([IdSanPhamNavigationIdSanPham]) REFERENCES [SanPhams] ([IdSanPham])
);
GO


CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO


CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO


CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO


CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO


CREATE INDEX [IX_AspNetUsers_IdPhanQuyen] ON [AspNetUsers] ([IdPhanQuyen]);
GO


CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO


CREATE INDEX [IX_ChiTietDonHangs_IdDonHangNavigationIdDonHang] ON [ChiTietDonHangs] ([IdDonHangNavigationIdDonHang]);
GO


CREATE INDEX [IX_ChiTietDonHangs_IdSanPhamNavigationIdSanPham] ON [ChiTietDonHangs] ([IdSanPhamNavigationIdSanPham]);
GO


CREATE INDEX [IX_DanhSachYeuThiches_IdNguoiDung] ON [DanhSachYeuThiches] ([IdNguoiDung]);
GO


CREATE INDEX [IX_DanhSachYeuThiches_IdSanPham] ON [DanhSachYeuThiches] ([IdSanPham]);
GO


CREATE INDEX [IX_DonHangs_IdCoupon] ON [DonHangs] ([IdCoupon]);
GO


CREATE INDEX [IX_DonHangs_IdNguoiDung] ON [DonHangs] ([IdNguoiDung]);
GO


CREATE INDEX [IX_DonHangs_IdVanChuyen] ON [DonHangs] ([IdVanChuyen]);
GO


CREATE INDEX [IX_LichSuMuaHangs_IdNguoiDung] ON [LichSuMuaHangs] ([IdNguoiDung]);
GO


CREATE INDEX [IX_LichSuMuaHangs_IdSanPham] ON [LichSuMuaHangs] ([IdSanPham]);
GO


CREATE INDEX [IX_LichSuMuaHangs_SanPhamIdSanPham] ON [LichSuMuaHangs] ([SanPhamIdSanPham]);
GO


CREATE INDEX [IX_SanPhams_IdDanhMuc] ON [SanPhams] ([IdDanhMuc]);
GO


CREATE INDEX [IX_SanPhams_IdDanhMucNavigationIdDanhMuc] ON [SanPhams] ([IdDanhMucNavigationIdDanhMuc]);
GO


CREATE INDEX [IX_SanPhams_IdThuongHieu] ON [SanPhams] ([IdThuongHieu]);
GO


CREATE INDEX [IX_SanPhams_IdThuongHieuNavigationIdThuongHieu] ON [SanPhams] ([IdThuongHieuNavigationIdThuongHieu]);
GO


