using BlueSchoolSystem.Models;
using BlueSchoolSystem.Repository;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlueSchoolSystem.Tests;

/// <summary>
/// Integration tests cho CRUD Sinh Viên
/// GET  /api/laydanhsachsinhvien          – Admin
/// POST /api/taomoisinhvien               – Admin
/// PUT  /api/suathongtinsinhvien/{mssv}   – Admin
/// GET  /api/laysinhvientheomalop/{maLop} – Admin
/// </summary>
public class StudentIntegrationTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly SeededWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public StudentIntegrationTests(SeededWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── Tạo JWT token test helpers ───────────────────────────────────────────
    private void AuthAsAdmin() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Admin, SeededWebApplicationFactory.AdminUserName));

    private void AuthAsTeacher() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Teacher, SeededWebApplicationFactory.TeacherUserName));

    private void AuthAsStudent() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Student, SeededWebApplicationFactory.StudentUserName));

    private void Deauth() => _client.DefaultRequestHeaders.Authorization = null;

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-01: Lấy danh sách sinh viên (Admin) → 200 + có dữ liệu
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetAllStudents_AdminToken_Returns200WithData()
    {
        // Arrange
        AuthAsAdmin();

        // Act
        var response = await _client.GetAsync("/api/laydanhsachsinhvien");
        var body     = await ParseBodyAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("code").GetInt32().Should().Be(200);
        body.GetProperty("soluongsinhvien").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        var data = body.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-02: Lấy danh sách sinh viên không có token → 401
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetAllStudents_NoToken_Returns401()
    {
        Deauth();
        var response = await _client.GetAsync("/api/laydanhsachsinhvien");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-03: Lấy danh sách sinh viên với role Teacher → 403
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetAllStudents_TeacherToken_Returns403()
    {
        AuthAsTeacher();
        var response = await _client.GetAsync("/api/laydanhsachsinhvien");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-04: Lấy sinh viên theo mã lớp hợp lệ → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetStudentsByClass_ValidClass_Returns200()
    {
        AuthAsAdmin();
        var response = await _client.GetAsync($"/api/laysinhvientheomalop/{SeededWebApplicationFactory.SeededMaLop}");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("soluongsinhvien").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-05: Lấy sinh viên theo mã lớp không tồn tại → 404
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetStudentsByClass_InvalidClass_Returns404()
    {
        AuthAsAdmin();
        var response = await _client.GetAsync("/api/laysinhvientheomalop/KHONGTONTAI999");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("result").GetBoolean().Should().BeFalse();
        body.GetProperty("code").GetInt32().Should().Be(404);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-06: Tạo sinh viên mới thành công → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateStudent_ValidData_Returns200()
    {
        // Arrange
        AuthAsAdmin();

        // MSSV phải là 10-15 chữ số (^[0-9]+$)
        var mssv = $"2023{Math.Abs(Guid.NewGuid().GetHashCode()) % 1_000_000:D6}";
        var payload = new
        {
            UserName    = $"sv_new_{mssv.ToLower()}",
            Email       = $"sv_{mssv.ToLower()}@test.com",
            PhoneNumber = "0987654321",
            Password    = "Student@Test123!",
            NganhHocId  = (string?)null,
            Student     = new
            {
                MSSV          = mssv,
                HoVaTenDem    = "Nguyen Van",
                Ten           = "Moi",
                CCCD          = "111111111119",
                NgaySinh      = "2003-01-01",
                GioiTinh      = true,
                DiaChi        = "789 Test Street",
                LopId         = SeededWebApplicationFactory.SeededLopId,
                NgayNhapHoc   = "2023-09-01",
                NgayTotNghiep = "2027-06-01",
                TrangThaiId   = 1
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/taomoisinhvien", payload);
        var body     = await ParseBodyAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("code").GetInt32().Should().Be(200);
        body.GetProperty("studentId").GetInt32().Should().BeGreaterThan(0);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-07: Tạo sinh viên với role Teacher → 403
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateStudent_TeacherToken_Returns403()
    {
        AuthAsTeacher();
        var payload = new { UserName = "sv_x", Email = "sv_x@test.com", Password = "P@ss1234!" };
        var response = await _client.PostAsJsonAsync("/api/taomoisinhvien", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-08: Tạo sinh viên thiếu trường bắt buộc → 400
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateStudent_MissingRequiredFields_Returns400()
    {
        AuthAsAdmin();

        // Payload thiếu Password
        var payload = new
        {
            UserName = "sv_missing_pass",
            Email    = "sv_missing@test.com",
            Student  = new
            {
                MSSV          = "9999999999",
                HoVaTenDem    = "X",
                Ten           = "X",
                CCCD          = "123",         // không đúng format (< 12 số)
                NgaySinh      = "2000-01-01",
                GioiTinh      = true,
                DiaChi        = "Addr",
                NgayNhapHoc   = "2020-09-01",
                NgayTotNghiep = "2024-06-01",
                TrangThaiId   = 1
            }
        };

        var response = await _client.PostAsJsonAsync("/api/taomoisinhvien", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-09: Cập nhật thông tin sinh viên (Admin) → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task UpdateStudent_ValidData_Returns200()
    {
        AuthAsAdmin();

        var payload = new
        {
            GhiChu = "Updated via integration test",
            TrangThaiId = 1
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/suathongtinsinhvien/{SeededWebApplicationFactory.StudentMSSV}", payload);
        var body = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("message").GetString().Should().Contain("thành công");

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-10: Cập nhật sinh viên không tồn tại → 404
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task UpdateStudent_NotFound_Returns404()
    {
        AuthAsAdmin();

        var payload   = new { GhiChu = "ghost" };
        var response  = await _client.PutAsJsonAsync("/api/suathongtinsinhvien/MSSV_KHONG_TON_TAI", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-SV-11: Lọc sinh viên theo từ khóa (Admin) → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task FilterStudents_ByKeyword_Returns200()
    {
        AuthAsAdmin();

        var response = await _client.GetAsync("/api/timkiemsinhvien?keyword=Test");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();

        Deauth();
    }

    // ─── Helper ─────────────────────────────────────────────────────────────
    private static async Task<JsonElement> ParseBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
