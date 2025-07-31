using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawVerse.Migrations
{
    /// <inheritdoc />
    public partial class updatedatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "ThuongHieus",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "Hoạt động",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "Hoạt động");

            migrationBuilder.AlterColumn<string>(
                name: "TenAlias",
                table: "ThuongHieus",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Logo",
                table: "ThuongHieus",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "DanhMucs",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "Đang bán",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "Đang bán");

            migrationBuilder.AlterColumn<string>(
                name: "HinhAnh",
                table: "DanhMucs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "ThuongHieus",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Hoạt động",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldDefaultValue: "Hoạt động");

            migrationBuilder.AlterColumn<string>(
                name: "TenAlias",
                table: "ThuongHieus",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Logo",
                table: "ThuongHieus",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "DanhMucs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Đang bán",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldDefaultValue: "Đang bán");

            migrationBuilder.AlterColumn<string>(
                name: "HinhAnh",
                table: "DanhMucs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
