using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawVerse.Migrations
{
    /// <inheritdoc />
    public partial class AddPhiVanChuyenToDonHangs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IdSanPhamNavigationIdSanPham",
                table: "ChiTietDonHangs",
                newName: "SanPhamIdSanPham");

            migrationBuilder.RenameIndex(
                name: "IX_ChiTietDonHangs_IdSanPhamNavigationIdSanPham",
                table: "ChiTietDonHangs",
                newName: "IX_ChiTietDonHangs_SanPhamIdSanPham");

            migrationBuilder.AddColumn<decimal>(
                name: "PhiVanChuyen",
                table: "DonHangs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhiVanChuyen",
                table: "DonHangs");

            migrationBuilder.RenameColumn(
                name: "SanPhamIdSanPham",
                table: "ChiTietDonHangs",
                newName: "IdSanPhamNavigationIdSanPham");

            migrationBuilder.RenameIndex(
                name: "IX_ChiTietDonHangs_SanPhamIdSanPham",
                table: "ChiTietDonHangs",
                newName: "IX_ChiTietDonHangs_IdSanPhamNavigationIdSanPham");
        }
    }
}
