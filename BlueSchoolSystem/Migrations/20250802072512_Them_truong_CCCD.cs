using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class Them_truong_CCCD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CCCD",
                table: "SinhViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CCCD",
                table: "GiangViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CCCD",
                table: "SinhViens");

            migrationBuilder.DropColumn(
                name: "CCCD",
                table: "GiangViens");
        }
    }
}
