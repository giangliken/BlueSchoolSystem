using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueSchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class Install : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Device = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FcmToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaceRegistered = table.Column<bool>(type: "bit", nullable: false),
                    FaceRegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FaceNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CoSos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCoSo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenCoSo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoSos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaceVerifyLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceVerifyLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HocKys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenHocKy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocKys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Khoas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhoa = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TenKhoa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Khoas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonHocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaMonHoc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenMonHoc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoTinChi = table.Column<int>(type: "int", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonHocs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetOTPs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OTPCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetOTPs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrangThais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenTrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiTrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrangThais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThongBaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiverUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SenderUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongBaos_AspNetUsers_ReceiverUserId",
                        column: x => x.ReceiverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFaceTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Embedding = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFaceTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFaceTemplates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhongHocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhongHoc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenPhongHoc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoChoNgoi = table.Column<int>(type: "int", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoSoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongHocs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhongHocs_CoSos_CoSoId",
                        column: x => x.CoSoId,
                        principalTable: "CoSos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NganhHocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNganh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenNganh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KhoaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NganhHocs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NganhHocs_Khoas_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiangViens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGiangVien = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoVaTenDem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CCCD = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioiTinh = table.Column<bool>(type: "bit", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThaiId = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KhoaId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiangViens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiangViens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GiangViens_Khoas_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiangViens_TrangThais_TrangThaiId",
                        column: x => x.TrangThaiId,
                        principalTable: "TrangThais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LopHocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLop = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TenLop = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NganhId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LopHocs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LopHocs_NganhHocs_NganhId",
                        column: x => x.NganhId,
                        principalTable: "NganhHocs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MonHocNganhHoc",
                columns: table => new
                {
                    MonHocsId = table.Column<int>(type: "int", nullable: false),
                    NganhHocsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonHocNganhHoc", x => new { x.MonHocsId, x.NganhHocsId });
                    table.ForeignKey(
                        name: "FK_MonHocNganhHoc_MonHocs_MonHocsId",
                        column: x => x.MonHocsId,
                        principalTable: "MonHocs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonHocNganhHoc_NganhHocs_NganhHocsId",
                        column: x => x.NganhHocsId,
                        principalTable: "NganhHocs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietKhoaViens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoaId = table.Column<int>(type: "int", nullable: false),
                    TruongKhoaId = table.Column<int>(type: "int", nullable: true),
                    PhoKhoaId = table.Column<int>(type: "int", nullable: true),
                    TroLiKhoaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietKhoaViens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietKhoaViens_GiangViens_PhoKhoaId",
                        column: x => x.PhoKhoaId,
                        principalTable: "GiangViens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChiTietKhoaViens_GiangViens_TroLiKhoaId",
                        column: x => x.TroLiKhoaId,
                        principalTable: "GiangViens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChiTietKhoaViens_GiangViens_TruongKhoaId",
                        column: x => x.TruongKhoaId,
                        principalTable: "GiangViens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChiTietKhoaViens_Khoas_KhoaId",
                        column: x => x.KhoaId,
                        principalTable: "Khoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiangVienMonHoc",
                columns: table => new
                {
                    GiangViensId = table.Column<int>(type: "int", nullable: false),
                    MonHocsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiangVienMonHoc", x => new { x.GiangViensId, x.MonHocsId });
                    table.ForeignKey(
                        name: "FK_GiangVienMonHoc_GiangViens_GiangViensId",
                        column: x => x.GiangViensId,
                        principalTable: "GiangViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiangVienMonHoc_MonHocs_MonHocsId",
                        column: x => x.MonHocsId,
                        principalTable: "MonHocs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LopHocPhans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLopHocPhan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenLopHocPhan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonHocId = table.Column<int>(type: "int", nullable: false),
                    GiangVienId = table.Column<int>(type: "int", nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SiSo = table.Column<int>(type: "int", nullable: false),
                    TrangThaiId = table.Column<int>(type: "int", nullable: false),
                    HocKyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LopHocPhans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LopHocPhans_GiangViens_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "GiangViens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LopHocPhans_HocKys_HocKyId",
                        column: x => x.HocKyId,
                        principalTable: "HocKys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LopHocPhans_MonHocs_MonHocId",
                        column: x => x.MonHocId,
                        principalTable: "MonHocs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LopHocPhans_TrangThais_TrangThaiId",
                        column: x => x.TrangThaiId,
                        principalTable: "TrangThais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SinhViens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MSSV = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    HoVaTenDem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CCCD = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioiTinh = table.Column<bool>(type: "bit", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LopId = table.Column<int>(type: "int", nullable: true),
                    NgayNhapHoc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayTotNghiep = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThaiId = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinhViens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SinhViens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SinhViens_LopHocs_LopId",
                        column: x => x.LopId,
                        principalTable: "LopHocs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SinhViens_TrangThais_TrangThaiId",
                        column: x => x.TrangThaiId,
                        principalTable: "TrangThais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiemDanhs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LopHocPhanId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThaiId = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiemDanhs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiemDanhs_LopHocPhans_LopHocPhanId",
                        column: x => x.LopHocPhanId,
                        principalTable: "LopHocPhans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiemDanhs_TrangThais_TrangThaiId",
                        column: x => x.TrangThaiId,
                        principalTable: "TrangThais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichHocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LopHocPhanId = table.Column<int>(type: "int", nullable: false),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioBatDau = table.Column<TimeSpan>(type: "time", nullable: false),
                    GioKetThuc = table.Column<TimeSpan>(type: "time", nullable: false),
                    PhongHocId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichHocs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichHocs_LopHocPhans_LopHocPhanId",
                        column: x => x.LopHocPhanId,
                        principalTable: "LopHocPhans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichHocs_PhongHocs_PhongHocId",
                        column: x => x.PhongHocId,
                        principalTable: "PhongHocs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LichThis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LopHocPhanId = table.Column<int>(type: "int", nullable: false),
                    NgayThi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioBatDau = table.Column<TimeSpan>(type: "time", nullable: false),
                    GioKetThuc = table.Column<TimeSpan>(type: "time", nullable: false),
                    HinhThucThi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhongHocId = table.Column<int>(type: "int", nullable: false),
                    TrangThaiId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichThis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichThis_LopHocPhans_LopHocPhanId",
                        column: x => x.LopHocPhanId,
                        principalTable: "LopHocPhans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichThis_PhongHocs_PhongHocId",
                        column: x => x.PhongHocId,
                        principalTable: "PhongHocs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichThis_TrangThais_TrangThaiId",
                        column: x => x.TrangThaiId,
                        principalTable: "TrangThais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BangDiems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SinhVienId = table.Column<int>(type: "int", nullable: false),
                    LopHocPhanId = table.Column<int>(type: "int", nullable: false),
                    DiemChuyenCan = table.Column<float>(type: "real", nullable: true),
                    DiemCuoiKy = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BangDiems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BangDiems_LopHocPhans_LopHocPhanId",
                        column: x => x.LopHocPhanId,
                        principalTable: "LopHocPhans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BangDiems_SinhViens_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietLopHocPhans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LopHocPhanId = table.Column<int>(type: "int", nullable: false),
                    SinhVienId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietLopHocPhans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietLopHocPhans_LopHocPhans_LopHocPhanId",
                        column: x => x.LopHocPhanId,
                        principalTable: "LopHocPhans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietLopHocPhans_SinhViens_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhViens",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietLopHocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LopHocId = table.Column<int>(type: "int", nullable: false),
                    GiangVienId = table.Column<int>(type: "int", nullable: true),
                    LopTruongId = table.Column<int>(type: "int", nullable: true),
                    LopPhoId = table.Column<int>(type: "int", nullable: true),
                    BiThuId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietLopHocs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietLopHocs_GiangViens_GiangVienId",
                        column: x => x.GiangVienId,
                        principalTable: "GiangViens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChiTietLopHocs_LopHocs_LopHocId",
                        column: x => x.LopHocId,
                        principalTable: "LopHocs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietLopHocs_SinhViens_BiThuId",
                        column: x => x.BiThuId,
                        principalTable: "SinhViens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChiTietLopHocs_SinhViens_LopPhoId",
                        column: x => x.LopPhoId,
                        principalTable: "SinhViens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChiTietLopHocs_SinhViens_LopTruongId",
                        column: x => x.LopTruongId,
                        principalTable: "SinhViens",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DangKyHocPhans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SinhVienId = table.Column<int>(type: "int", nullable: false),
                    HocPhanId = table.Column<int>(type: "int", nullable: false),
                    LopHocPhanId = table.Column<int>(type: "int", nullable: true),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangKyHocPhans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DangKyHocPhans_LopHocPhans_LopHocPhanId",
                        column: x => x.LopHocPhanId,
                        principalTable: "LopHocPhans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DangKyHocPhans_SinhViens_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDiemDanhs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiemDanhId = table.Column<int>(type: "int", nullable: false),
                    SinhVienId = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    TrangThaiId = table.Column<int>(type: "int", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDiemDanhs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietDiemDanhs_DiemDanhs_DiemDanhId",
                        column: x => x.DiemDanhId,
                        principalTable: "DiemDanhs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDiemDanhs_SinhViens_SinhVienId",
                        column: x => x.SinhVienId,
                        principalTable: "SinhViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietDiemDanhs_TrangThais_TrangThaiId",
                        column: x => x.TrangThaiId,
                        principalTable: "TrangThais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BangDiems_LopHocPhanId",
                table: "BangDiems",
                column: "LopHocPhanId");

            migrationBuilder.CreateIndex(
                name: "IX_BangDiems_SinhVienId",
                table: "BangDiems",
                column: "SinhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDiemDanhs_DiemDanhId",
                table: "ChiTietDiemDanhs",
                column: "DiemDanhId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDiemDanhs_SinhVienId",
                table: "ChiTietDiemDanhs",
                column: "SinhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDiemDanhs_TrangThaiId",
                table: "ChiTietDiemDanhs",
                column: "TrangThaiId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKhoaViens_KhoaId",
                table: "ChiTietKhoaViens",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKhoaViens_PhoKhoaId",
                table: "ChiTietKhoaViens",
                column: "PhoKhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKhoaViens_TroLiKhoaId",
                table: "ChiTietKhoaViens",
                column: "TroLiKhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKhoaViens_TruongKhoaId",
                table: "ChiTietKhoaViens",
                column: "TruongKhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietLopHocPhans_LopHocPhanId",
                table: "ChiTietLopHocPhans",
                column: "LopHocPhanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietLopHocPhans_SinhVienId",
                table: "ChiTietLopHocPhans",
                column: "SinhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietLopHocs_BiThuId",
                table: "ChiTietLopHocs",
                column: "BiThuId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietLopHocs_GiangVienId",
                table: "ChiTietLopHocs",
                column: "GiangVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietLopHocs_LopHocId",
                table: "ChiTietLopHocs",
                column: "LopHocId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietLopHocs_LopPhoId",
                table: "ChiTietLopHocs",
                column: "LopPhoId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietLopHocs_LopTruongId",
                table: "ChiTietLopHocs",
                column: "LopTruongId");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyHocPhans_LopHocPhanId",
                table: "DangKyHocPhans",
                column: "LopHocPhanId");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyHocPhans_SinhVienId",
                table: "DangKyHocPhans",
                column: "SinhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_LopHocPhanId",
                table: "DiemDanhs",
                column: "LopHocPhanId");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_TrangThaiId",
                table: "DiemDanhs",
                column: "TrangThaiId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceVerifyLogs_UserId_At",
                table: "FaceVerifyLogs",
                columns: new[] { "UserId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_GiangVienMonHoc_MonHocsId",
                table: "GiangVienMonHoc",
                column: "MonHocsId");

            migrationBuilder.CreateIndex(
                name: "IX_GiangViens_KhoaId",
                table: "GiangViens",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_GiangViens_TrangThaiId",
                table: "GiangViens",
                column: "TrangThaiId");

            migrationBuilder.CreateIndex(
                name: "IX_GiangViens_UserId",
                table: "GiangViens",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LichHocs_LopHocPhanId",
                table: "LichHocs",
                column: "LopHocPhanId");

            migrationBuilder.CreateIndex(
                name: "IX_LichHocs_PhongHocId",
                table: "LichHocs",
                column: "PhongHocId");

            migrationBuilder.CreateIndex(
                name: "IX_LichThis_LopHocPhanId",
                table: "LichThis",
                column: "LopHocPhanId");

            migrationBuilder.CreateIndex(
                name: "IX_LichThis_PhongHocId",
                table: "LichThis",
                column: "PhongHocId");

            migrationBuilder.CreateIndex(
                name: "IX_LichThis_TrangThaiId",
                table: "LichThis",
                column: "TrangThaiId");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhans_GiangVienId",
                table: "LopHocPhans",
                column: "GiangVienId");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhans_HocKyId",
                table: "LopHocPhans",
                column: "HocKyId");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhans_MonHocId",
                table: "LopHocPhans",
                column: "MonHocId");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocPhans_TrangThaiId",
                table: "LopHocPhans",
                column: "TrangThaiId");

            migrationBuilder.CreateIndex(
                name: "IX_LopHocs_NganhId",
                table: "LopHocs",
                column: "NganhId");

            migrationBuilder.CreateIndex(
                name: "IX_MonHocNganhHoc_NganhHocsId",
                table: "MonHocNganhHoc",
                column: "NganhHocsId");

            migrationBuilder.CreateIndex(
                name: "IX_NganhHocs_KhoaId",
                table: "NganhHocs",
                column: "KhoaId");

            migrationBuilder.CreateIndex(
                name: "IX_PhongHocs_CoSoId",
                table: "PhongHocs",
                column: "CoSoId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhViens_LopId",
                table: "SinhViens",
                column: "LopId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhViens_TrangThaiId",
                table: "SinhViens",
                column: "TrangThaiId");

            migrationBuilder.CreateIndex(
                name: "IX_SinhViens_UserId",
                table: "SinhViens",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaos_ReceiverUserId",
                table: "ThongBaos",
                column: "ReceiverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFaceTemplates_UserId_IsActive",
                table: "UserFaceTemplates",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BangDiems");

            migrationBuilder.DropTable(
                name: "ChiTietDiemDanhs");

            migrationBuilder.DropTable(
                name: "ChiTietKhoaViens");

            migrationBuilder.DropTable(
                name: "ChiTietLopHocPhans");

            migrationBuilder.DropTable(
                name: "ChiTietLopHocs");

            migrationBuilder.DropTable(
                name: "DangKyHocPhans");

            migrationBuilder.DropTable(
                name: "FaceVerifyLogs");

            migrationBuilder.DropTable(
                name: "GiangVienMonHoc");

            migrationBuilder.DropTable(
                name: "LichHocs");

            migrationBuilder.DropTable(
                name: "LichThis");

            migrationBuilder.DropTable(
                name: "MonHocNganhHoc");

            migrationBuilder.DropTable(
                name: "PasswordResetOTPs");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ThongBaos");

            migrationBuilder.DropTable(
                name: "UserFaceTemplates");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "DiemDanhs");

            migrationBuilder.DropTable(
                name: "SinhViens");

            migrationBuilder.DropTable(
                name: "PhongHocs");

            migrationBuilder.DropTable(
                name: "LopHocPhans");

            migrationBuilder.DropTable(
                name: "LopHocs");

            migrationBuilder.DropTable(
                name: "CoSos");

            migrationBuilder.DropTable(
                name: "GiangViens");

            migrationBuilder.DropTable(
                name: "HocKys");

            migrationBuilder.DropTable(
                name: "MonHocs");

            migrationBuilder.DropTable(
                name: "NganhHocs");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "TrangThais");

            migrationBuilder.DropTable(
                name: "Khoas");
        }
    }
}
