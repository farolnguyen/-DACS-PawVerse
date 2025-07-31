using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawVerse.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__SanPham__ID_Danh__5812160E",
                table: "SanPhams");

            migrationBuilder.DropForeignKey(
                name: "FK__SanPham__ID_Thuo__59063A47",
                table: "SanPhams");

            migrationBuilder.AlterColumn<string>(
                name: "XuatXu",
                table: "SanPhams",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrongLuong",
                table: "SanPhams",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "SanPhams",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Còn hàng",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "Còn hàng");

            migrationBuilder.AlterColumn<string>(
                name: "TenSanPham",
                table: "SanPhams",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TenAlias",
                table: "SanPhams",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NgaySanXuat",
                table: "SanPhams",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "MauSac",
                table: "SanPhams",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HinhAnh",
                table: "SanPhams",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "HanSuDung",
                table: "SanPhams",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.CreateTable(
                name: "GioHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioHangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GioHangs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GioHangChiTiets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GioHangId = table.Column<int>(type: "int", nullable: false),
                    SanPhamId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioHangChiTiets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GioHangChiTiets_GioHangs_GioHangId",
                        column: x => x.GioHangId,
                        principalTable: "GioHangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GioHangChiTiets_SanPhams_SanPhamId",
                        column: x => x.SanPhamId,
                        principalTable: "SanPhams",
                        principalColumn: "IdSanPham",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IdDanhMuc",
                table: "SanPhams",
                column: "IdDanhMuc");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IdThuongHieu",
                table: "SanPhams",
                column: "IdThuongHieu");

            migrationBuilder.CreateIndex(
                name: "IX_GioHangChiTiets_GioHangId",
                table: "GioHangChiTiets",
                column: "GioHangId");

            migrationBuilder.CreateIndex(
                name: "IX_GioHangChiTiets_SanPhamId",
                table: "GioHangChiTiets",
                column: "SanPhamId");

            migrationBuilder.CreateIndex(
                name: "IX_GioHangs_UserId",
                table: "GioHangs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SanPham_DanhMuc_Extra",
                table: "SanPhams",
                column: "IdDanhMucNavigationIdDanhMuc",
                principalTable: "DanhMucs",
                principalColumn: "IdDanhMuc");

            migrationBuilder.AddForeignKey(
                name: "FK_SanPham_ThuongHieu_Extra",
                table: "SanPhams",
                column: "IdThuongHieuNavigationIdThuongHieu",
                principalTable: "ThuongHieus",
                principalColumn: "IdThuongHieu");

            migrationBuilder.AddForeignKey(
                name: "FK__SanPham__ID_Danh__5812160E",
                table: "SanPhams",
                column: "IdDanhMuc",
                principalTable: "DanhMucs",
                principalColumn: "IdDanhMuc");

            migrationBuilder.AddForeignKey(
                name: "FK__SanPham__ID_Thuo__59063A47",
                table: "SanPhams",
                column: "IdThuongHieu",
                principalTable: "ThuongHieus",
                principalColumn: "IdThuongHieu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SanPham_DanhMuc_Extra",
                table: "SanPhams");

            migrationBuilder.DropForeignKey(
                name: "FK_SanPham_ThuongHieu_Extra",
                table: "SanPhams");

            migrationBuilder.DropForeignKey(
                name: "FK__SanPham__ID_Danh__5812160E",
                table: "SanPhams");

            migrationBuilder.DropForeignKey(
                name: "FK__SanPham__ID_Thuo__59063A47",
                table: "SanPhams");

            migrationBuilder.DropTable(
                name: "GioHangChiTiets");

            migrationBuilder.DropTable(
                name: "GioHangs");

            migrationBuilder.DropIndex(
                name: "IX_SanPhams_IdDanhMuc",
                table: "SanPhams");

            migrationBuilder.DropIndex(
                name: "IX_SanPhams_IdThuongHieu",
                table: "SanPhams");

            migrationBuilder.AlterColumn<string>(
                name: "XuatXu",
                table: "SanPhams",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrongLuong",
                table: "SanPhams",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "SanPhams",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Còn hàng",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Còn hàng");

            migrationBuilder.AlterColumn<string>(
                name: "TenSanPham",
                table: "SanPhams",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "TenAlias",
                table: "SanPhams",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "NgaySanXuat",
                table: "SanPhams",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "MauSac",
                table: "SanPhams",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HinhAnh",
                table: "SanPhams",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "HanSuDung",
                table: "SanPhams",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddForeignKey(
                name: "FK__SanPham__ID_Danh__5812160E",
                table: "SanPhams",
                column: "IdDanhMucNavigationIdDanhMuc",
                principalTable: "DanhMucs",
                principalColumn: "IdDanhMuc");

            migrationBuilder.AddForeignKey(
                name: "FK__SanPham__ID_Thuo__59063A47",
                table: "SanPhams",
                column: "IdThuongHieuNavigationIdThuongHieu",
                principalTable: "ThuongHieus",
                principalColumn: "IdThuongHieu");
        }
    }
}
