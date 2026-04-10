using BlueSchoolSystem.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlueSchoolSystem.Tests;

/// <summary>
/// Integration tests cho CRUD Lớp Học
/// GET  /api/laydanhsachlophoc             – Admin
/// GET  /api/laydanhsachlophoctheonganh    – Admin
/// GET  /api/laychitietlophoc?id={id}      – Admin
/// POST /api/themlophoc                    – Admin
/// </summary>
public class ClassIntegrationTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly SeededWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClassIntegrationTests(SeededWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── Auth helpers ───────────────────────────────────────────────────────
    private void AuthAsAdmin() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Admin, SeededWebApplicationFactory.AdminUserName));

    private void AuthAsTeacher() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Teacher, SeededWebApplicationFactory.TeacherUserName));

    private void Deauth() => _client.DefaultRequestHeaders.Authorization = null;

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-01: Lấy danh sách lớp học (Admin) → 200 + có dữ liệu
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetAllClasses_AdminToken_Returns200WithData()
    {
        AuthAsAdmin();

        var response = await _client.GetAsync("/api/laydanhsachlophoc");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("code").GetInt32().Should().Be(200);
        body.GetProperty("soluonglop").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        var data = body.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        // Kiểm tra có lớp đã seed
        var firstClass = data[0];
        firstClass.TryGetProperty("maLop", out _).Should().BeTrue();

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-02: Lấy danh sách lớp học không có token → 401
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetAllClasses_NoToken_Returns401()
    {
        Deauth();
        var response = await _client.GetAsync("/api/laydanhsachlophoc");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-03: Lấy danh sách lớp học với role Teacher → 403
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetAllClasses_TeacherToken_Returns403()
    {
        AuthAsTeacher();
        var response = await _client.GetAsync("/api/laydanhsachlophoc");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-04: Lấy danh sách lớp theo ngành hợp lệ → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetClassesByMajor_ValidMajor_Returns200()
    {
        AuthAsAdmin();

        var response = await _client.GetAsync($"/api/laydanhsachlophoctheonganh?maNganh={SeededWebApplicationFactory.SeededMaNganh}");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("tongsoluongloptheonganh").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-05: Lấy danh sách lớp theo ngành không tồn tại → 404
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetClassesByMajor_InvalidMajor_Returns404()
    {
        AuthAsAdmin();

        var response = await _client.GetAsync("/api/laydanhsachlophoctheonganh?maNganh=KHONGTONTAI999");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("result").GetBoolean().Should().BeFalse();
        body.GetProperty("code").GetInt32().Should().Be(404);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-06: Lấy chi tiết lớp học theo Id hợp lệ → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetClassDetail_ValidId_Returns200()
    {
        AuthAsAdmin();

        var response = await _client.GetAsync($"/api/laychitietlophoc?id={SeededWebApplicationFactory.SeededLopId}");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();

        var data = body.GetProperty("data");
        data.GetProperty("maLop").GetString().Should().Be(SeededWebApplicationFactory.SeededMaLop);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-07: Lấy chi tiết lớp học theo Id không tồn tại → 404
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetClassDetail_InvalidId_Returns404()
    {
        AuthAsAdmin();

        var response = await _client.GetAsync("/api/laychitietlophoc?id=99999");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("result").GetBoolean().Should().BeFalse();

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-08: Tạo lớp học mới thành công → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateClass_ValidData_Returns200()
    {
        AuthAsAdmin();

        var uniqueCode = $"LOP_{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
        var payload = new
        {
            MaLop   = uniqueCode,
            TenLop  = $"Lớp Test {uniqueCode}",
            NganhId = SeededWebApplicationFactory.SeededNganhId,
            MoTa    = "Lớp tạo từ integration test"
        };

        var response = await _client.PostAsJsonAsync("/api/themlophoc", payload);
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("code").GetInt32().Should().Be(200);

        var data = body.GetProperty("data");
        data.GetProperty("maLop").GetString().Should().Be(uniqueCode);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-09: Tạo lớp học trùng mã → 409
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateClass_DuplicateMaLop_Returns409()
    {
        AuthAsAdmin();

        // Mã lớp đã được seed
        var payload = new
        {
            MaLop   = SeededWebApplicationFactory.SeededMaLop,
            TenLop  = "Lớp Trùng",
            NganhId = SeededWebApplicationFactory.SeededNganhId
        };

        var response = await _client.PostAsJsonAsync("/api/themlophoc", payload);
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body.GetProperty("result").GetBoolean().Should().BeFalse();
        body.GetProperty("code").GetInt32().Should().Be(409);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-10: Tạo lớp học với ngành không tồn tại → 404
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateClass_InvalidNganh_Returns404()
    {
        AuthAsAdmin();

        var payload = new
        {
            MaLop   = $"NEWLOP{Guid.NewGuid().ToString("N")[..4]}",
            TenLop  = "Lớp Ngành Không Tồn Tại",
            NganhId = 99999
        };

        var response = await _client.PostAsJsonAsync("/api/themlophoc", payload);
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("result").GetBoolean().Should().BeFalse();

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-11: Tạo lớp học với role Teacher → 403
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateClass_TeacherToken_Returns403()
    {
        AuthAsTeacher();

        var payload = new { MaLop = "XLOP01", TenLop = "X Lop", NganhId = 1 };
        var response = await _client.PostAsJsonAsync("/api/themlophoc", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-LH-12: Lọc lớp theo điều kiện → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetClassesByCondition_WithMaKhoa_Returns200WithData()
    {
        AuthAsAdmin();

        var response = await _client.GetAsync($"/api/laydanhsachlophoctheodieukien?maKhoa={SeededWebApplicationFactory.SeededMaKhoa}");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("soluonglop").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        Deauth();
    }

    // ─── Helper ─────────────────────────────────────────────────────────────
    private static async Task<JsonElement> ParseBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
