using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DangKyHocPhans_LopHocPhans_LopHocPhanId",
                table: "DangKyHocPhans");

            migrationBuilder.DropForeignKey(
                name: "FK_DangKyHocPhans_SinhViens_SinhVienId",
                table: "DangKyHocPhans");

            migrationBuilder.DropForeignKey(
                name: "FK_LopHocPhans_TrangThais_TrangThaiId",
                table: "LopHocPhans");

            migrationBuilder.DropColumn(
                name: "HocPhanId",
                table: "DangKyHocPhans");

            migrationBuilder.AddColumn<int>(
                name: "TrangThaiId",
                table: "HocKys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "LopHocPhanId",
                table: "DangKyHocPhans",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiDangKy",
                table: "DangKyHocPhans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DotDangKys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HocKyId = table.Column<int>(type: "int", nullable: false),
                    TenDot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoaiDoiTuong = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GiaTriDoiTuong = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiThaoTac = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotDangKys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DotDangKys_HocKys_HocKyId",
                        column: x => x.HocKyId,
                        principalTable: "HocKys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KhoaHocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NamHoc = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhoaHocs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChuongTrinhDaoTaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoaHocId = table.Column<int>(type: "int", nullable: false),
                    NganhHocId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuongTrinhDaoTaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChuongTrinhDaoTaos_KhoaHocs_KhoaHocId",
                        column: x => x.KhoaHocId,
                        principalTable: "KhoaHocs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChuongTrinhDaoTaos_NganhHocs_NganhHocId",
                        column: x => x.NganhHocId,
                        principalTable: "NganhHocs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietChuongTrinhDaoTaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChuongTrinhDaoTaoId = table.Column<int>(type: "int", nullable: false),
                    MaMonHoc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaMonHocTienQuyet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HocKy = table.Column<int>(type: "int", nullable: false),
                    BatBuoc = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietChuongTrinhDaoTaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietChuongTrinhDaoTaos_ChuongTrinhDaoTaos_ChuongTrinhDaoTaoId",
                        column: x => x.ChuongTrinhDaoTaoId,
                        principalTable: "ChuongTrinhDaoTaos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HocKys_TrangThaiId",
                table: "HocKys",
                column: "TrangThaiId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietChuongTrinhDaoTaos_ChuongTrinhDaoTaoId",
                table: "ChiTietChuongTrinhDaoTaos",
                column: "ChuongTrinhDaoTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ChuongTrinhDaoTaos_KhoaHocId",
                table: "ChuongTrinhDaoTaos",
                column: "KhoaHocId");

            migrationBuilder.CreateIndex(
                name: "IX_ChuongTrinhDaoTaos_NganhHocId",
                table: "ChuongTrinhDaoTaos",
                column: "NganhHocId");

            migrationBuilder.CreateIndex(
                name: "IX_DotDangKys_HocKyId",
                table: "DotDangKys",
                column: "HocKyId");

            migrationBuilder.AddForeignKey(
                name: "FK_DangKyHocPhans_LopHocPhans_LopHocPhanId",
                table: "DangKyHocPhans",
                column: "LopHocPhanId",
                principalTable: "LopHocPhans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DangKyHocPhans_SinhViens_SinhVienId",
                table: "DangKyHocPhans",
                column: "SinhVienId",
                principalTable: "SinhViens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HocKys_TrangThais_TrangThaiId",
                table: "HocKys",
                column: "TrangThaiId",
                principalTable: "TrangThais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LopHocPhans_TrangThais_TrangThaiId",
                table: "LopHocPhans",
                column: "TrangThaiId",
                principalTable: "TrangThais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DangKyHocPhans_LopHocPhans_LopHocPhanId",
                table: "DangKyHocPhans");

            migrationBuilder.DropForeignKey(
                name: "FK_DangKyHocPhans_SinhViens_SinhVienId",
                table: "DangKyHocPhans");

            migrationBuilder.DropForeignKey(
                name: "FK_HocKys_TrangThais_TrangThaiId",
                table: "HocKys");

            migrationBuilder.DropForeignKey(
                name: "FK_LopHocPhans_TrangThais_TrangThaiId",
                table: "LopHocPhans");

            migrationBuilder.DropTable(
                name: "ChiTietChuongTrinhDaoTaos");

            migrationBuilder.DropTable(
                name: "DotDangKys");

            migrationBuilder.DropTable(
                name: "ChuongTrinhDaoTaos");

            migrationBuilder.DropTable(
                name: "KhoaHocs");

            migrationBuilder.DropIndex(
                name: "IX_HocKys_TrangThaiId",
                table: "HocKys");

            migrationBuilder.DropColumn(
                name: "TrangThaiId",
                table: "HocKys");

            migrationBuilder.DropColumn(
                name: "LoaiDangKy",
                table: "DangKyHocPhans");

            migrationBuilder.AlterColumn<int>(
                name: "LopHocPhanId",
                table: "DangKyHocPhans",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "HocPhanId",
                table: "DangKyHocPhans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_DangKyHocPhans_LopHocPhans_LopHocPhanId",
                table: "DangKyHocPhans",
                column: "LopHocPhanId",
                principalTable: "LopHocPhans",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DangKyHocPhans_SinhViens_SinhVienId",
                table: "DangKyHocPhans",
                column: "SinhVienId",
                principalTable: "SinhViens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LopHocPhans_TrangThais_TrangThaiId",
                table: "LopHocPhans",
                column: "TrangThaiId",
                principalTable: "TrangThais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
