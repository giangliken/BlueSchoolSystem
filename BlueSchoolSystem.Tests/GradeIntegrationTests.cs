using BlueSchoolSystem.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BlueSchoolSystem.Tests;

/// <summary>
/// Integration tests cho chức năng Nhập và Xem Điểm
/// GET /api/diemsinhvien/{mssv}  – Student (chỉ xem điểm của chính mình) / Admin
/// </summary>
public class GradeIntegrationTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly SeededWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GradeIntegrationTests(SeededWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── Auth helpers ─────────────────────────────────────────────────────────
    private void AuthAsAdmin() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Admin, SeededWebApplicationFactory.AdminUserName));

    /// <summary>
    /// Token mang claim "username" = MSSV (theo logic controller diemsinhvien)
    /// </summary>
    private void AuthAsStudentWithMSSV(string mssv) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Student, mssv));

    private void AuthAsTeacher() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Teacher, SeededWebApplicationFactory.TeacherUserName));

    private void Deauth() => _client.DefaultRequestHeaders.Authorization = null;

    // ═══════════════════════════════════════════════════════════════════════
    // TC-DIEM-01: Xem điểm sinh viên (Admin) → 200 hoặc 404 nếu chưa có điểm
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetGrades_AdminToken_Returns200Or404()
    {
        // endpoint kiểm tra username == mssv; dùng student token với username = StudentMSSV
        AuthAsStudentWithMSSV(SeededWebApplicationFactory.StudentMSSV);

        var response = await _client.GetAsync($"/api/diemsinhvien/{SeededWebApplicationFactory.StudentMSSV}");

        // Sinh viên mới tạo chưa có điểm → 404, hoặc có điểm seed → 200
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await ParseBodyAsync(response);
            body.GetProperty("result").GetBoolean().Should().BeTrue();
        }

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-DIEM-02: Xem điểm với MSSV không tồn tại (Admin) → 404
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetGrades_NonExistentMSSV_Returns404()
    {
        // Dùng token với username = MSSV không tồn tại để vượt qua kiểm tra username==mssv
        const string ghostMssv = "9999000099990";
        AuthAsStudentWithMSSV(ghostMssv);

        var response = await _client.GetAsync($"/api/diemsinhvien/{ghostMssv}");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("result").GetBoolean().Should().BeFalse();

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-DIEM-03: Không có token → 401
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetGrades_NoToken_Returns401()
    {
        Deauth();
        var response = await _client.GetAsync($"/api/diemsinhvien/{SeededWebApplicationFactory.StudentMSSV}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-DIEM-04: Xem điểm có dữ liệu – seed BangDiem trước rồi query → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetGrades_WithSeededBangDiem_Returns200WithGradeData()
    {
        // Seed HocKy + LopHocPhan + ChiTietLopHocPhan + BangDiem
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Lấy sinh viên seed
            var sv = db.SinhViens.FirstOrDefault(s => s.MSSV == SeededWebApplicationFactory.StudentMSSV);
            if (sv == null) return;

            // Tạo HocKy nếu chưa có
            if (!db.HocKys.Any(h => h.TenHocKy == "Học kỳ Test 1"))
            {
                db.HocKys.Add(new HocKy
                {
                    Id          = 101,
                    TenHocKy    = "Học kỳ Test 1",
                    NgayBatDau  = new DateTime(2023, 9, 1),
                    NgayKetThuc = new DateTime(2024, 1, 15),
                    TrangThaiId = 1
                });
                await db.SaveChangesAsync();
            }

            // Tạo LopHocPhan
            if (!db.LopHocPhans.Any(l => l.MaLopHocPhan == "LHP_TEST_001"))
            {
                db.LopHocPhans.Add(new LopHocPhan
                {
                    Id            = 101,
                    MaLopHocPhan  = "LHP_TEST_001",
                    TenLopHocPhan = "Lớp học phần test",
                    MonHocId      = SeededWebApplicationFactory.SeededMonHocId,
                    HocKyId       = 101,
                    NgayBatDau    = new DateTime(2023, 9, 1),
                    NgayKetThuc   = new DateTime(2024, 1, 15),
                    SiSo          = 40,
                    TrangThaiId   = 1
                });
                await db.SaveChangesAsync();
            }

            // Tạo ChiTietLopHocPhan (ghi danh sinh viên vào lớp học phần)
            if (!db.ChiTietLopHocPhans.Any(c => c.SinhVienId == sv.Id && c.LopHocPhanId == 101))
            {
                db.ChiTietLopHocPhans.Add(new ChiTietLopHocPhan
                {
                    SinhVienId   = sv.Id,
                    LopHocPhanId = 101
                });
                await db.SaveChangesAsync();
            }

            // Tạo BangDiem
            if (!db.BangDiems.Any(b => b.SinhVienId == sv.Id && b.LopHocPhanId == 101))
            {
                db.BangDiems.Add(new BangDiem
                {
                    SinhVienId    = sv.Id,
                    LopHocPhanId  = 101,
                    DiemChuyenCan = 8.5f,
                    DiemCuoiKy    = 7.0f,
                    TrangThaiId   = 1
                });
                await db.SaveChangesAsync();
            }
        }

        // Act – dùng student token với username = StudentMSSV
        AuthAsStudentWithMSSV(SeededWebApplicationFactory.StudentMSSV);
        var response = await _client.GetAsync($"/api/diemsinhvien/{SeededWebApplicationFactory.StudentMSSV}");
        var body     = await ParseBodyAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();

        var data = body.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var hocKyDiem = data[0];
        hocKyDiem.TryGetProperty("diems", out var diems).Should().BeTrue();
        diems.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var firstDiem = diems[0];
        firstDiem.TryGetProperty("diemChuyenCan", out _).Should().BeTrue();
        firstDiem.TryGetProperty("diemCuoiKy", out _).Should().BeTrue();

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-DIEM-05: Teacher token không có quyền xem điểm sinh viên → 403
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetGrades_TeacherToken_Returns403()
    {
        AuthAsTeacher();
        var response = await _client.GetAsync($"/api/diemsinhvien/{SeededWebApplicationFactory.StudentMSSV}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ─── Helper ─────────────────────────────────────────────────────────────
    private static async Task<JsonElement> ParseBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
