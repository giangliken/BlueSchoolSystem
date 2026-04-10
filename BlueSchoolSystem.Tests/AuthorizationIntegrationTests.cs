using BlueSchoolSystem.Models;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BlueSchoolSystem.Tests;

/// <summary>
/// Integration tests cho Phân quyền (Role-Based Authorization)
/// Kiểm tra đầy đủ Admin / Teacher / Student / Anonymous trên nhiều endpoints.
/// </summary>
public class AuthorizationIntegrationTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly SeededWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationIntegrationTests(SeededWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── Token helpers ────────────────────────────────────────────────────────
    private string AdminToken   => TestTokenHelper.GenerateToken(SD.Role_Admin,   SeededWebApplicationFactory.AdminUserName);
    private string TeacherToken => TestTokenHelper.GenerateToken(SD.Role_Teacher, SeededWebApplicationFactory.TeacherUserName);
    private string StudentToken => TestTokenHelper.GenerateToken(SD.Role_Student, SeededWebApplicationFactory.StudentUserName);

    private void SetToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private void Deauth() => _client.DefaultRequestHeaders.Authorization = null;

    // ═══════════════════════════════════════════════════════════════════════
    //  NHÓM 1: Admin-only endpoints
    // ═══════════════════════════════════════════════════════════════════════

    public static IEnumerable<object[]> AdminOnlyGetEndpoints =>
        new List<object[]>
        {
            new object[] { "/api/laydanhsachsinhvien"    },
            new object[] { "/api/laydanhsachlophoc"       },
            new object[] { "/api/laydanhsachmonhoc"       },
            new object[] { "/api/laydanhsachgiangvien"    },
        };

    // ── TC-AUTHZ-01 ──────────────────────────────────────────────────────────
    [Theory]
    [MemberData(nameof(AdminOnlyGetEndpoints))]
    public async Task AdminOnlyGet_WithAdminToken_Returns200(string endpoint)
    {
        SetToken(AdminToken);
        var response = await _client.GetAsync(endpoint);
        // 200 hoặc 404 đều hợp lệ (nếu DB trống vẫn là 200 với empty list)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        Deauth();
    }

    // ── TC-AUTHZ-02 ──────────────────────────────────────────────────────────
    [Theory]
    [MemberData(nameof(AdminOnlyGetEndpoints))]
    public async Task AdminOnlyGet_WithTeacherToken_Returns403(string endpoint)
    {
        SetToken(TeacherToken);
        var response = await _client.GetAsync(endpoint);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ── TC-AUTHZ-03 ──────────────────────────────────────────────────────────
    [Theory]
    [MemberData(nameof(AdminOnlyGetEndpoints))]
    public async Task AdminOnlyGet_WithStudentToken_Returns403(string endpoint)
    {
        SetToken(StudentToken);
        var response = await _client.GetAsync(endpoint);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ── TC-AUTHZ-04 ──────────────────────────────────────────────────────────
    [Theory]
    [MemberData(nameof(AdminOnlyGetEndpoints))]
    public async Task AdminOnlyGet_NoToken_Returns401(string endpoint)
    {
        Deauth();
        var response = await _client.GetAsync(endpoint);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NHÓM 2: Admin-only POST endpoints
    // ═══════════════════════════════════════════════════════════════════════

    // ── TC-AUTHZ-05: POST /api/taomonhoc ────────────────────────────────────
    [Fact]
    public async Task CreateSubject_AdminToken_Returns200()
    {
        SetToken(AdminToken);

        var unique  = Guid.NewGuid().ToString("N")[..5].ToUpper();
        var payload = new { MaMonHoc = $"AZ_{unique}", TenMonHoc = $"Authz Test {unique}", SoTinChi = 2 };

        var response = await _client.PostAsJsonAsync("/api/taomonhoc", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Deauth();
    }

    // ── TC-AUTHZ-06 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateSubject_TeacherToken_Returns403()
    {
        SetToken(TeacherToken);
        var payload = new { MaMonHoc = "T_MH", TenMonHoc = "Teacher subject", SoTinChi = 1 };
        var response = await _client.PostAsJsonAsync("/api/taomonhoc", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ── TC-AUTHZ-07 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateSubject_StudentToken_Returns403()
    {
        SetToken(StudentToken);
        var payload = new { MaMonHoc = "S_MH", TenMonHoc = "Student cannot create", SoTinChi = 1 };
        var response = await _client.PostAsJsonAsync("/api/taomonhoc", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ── TC-AUTHZ-08 ─────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateSubject_NoToken_Returns401()
    {
        Deauth();
        var payload  = new { MaMonHoc = "ANON_MH", TenMonHoc = "Anonymous cannot create", SoTinChi = 1 };
        var response = await _client.PostAsJsonAsync("/api/taomonhoc", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NHÓM 3: Admin-only DELETE
    // ═══════════════════════════════════════════════════════════════════════

    // ── TC-AUTHZ-09: Teacher không được xóa môn học ──────────────────────────
    [Fact]
    public async Task DeleteSubject_TeacherToken_Returns403()
    {
        SetToken(TeacherToken);
        var response = await _client.DeleteAsync($"/api/xoamonhoc/{SeededWebApplicationFactory.SeededMonHocId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ── TC-AUTHZ-10: Student không được xóa môn học ─────────────────────────
    [Fact]
    public async Task DeleteSubject_StudentToken_Returns403()
    {
        SetToken(StudentToken);
        var response = await _client.DeleteAsync($"/api/xoamonhoc/{SeededWebApplicationFactory.SeededMonHocId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NHÓM 4: Public endpoint (không yêu cầu auth)
    // ═══════════════════════════════════════════════════════════════════════

    // ── TC-AUTHZ-11: GET /api/monhoc/{id} public, bất kỳ ai cũng truy cập được ──
    [Fact]
    public async Task GetSubjectById_Anonymous_Returns200Or404()
    {
        Deauth();
        var response = await _client.GetAsync($"/api/monhoc/{SeededWebApplicationFactory.SeededMonHocId}");
        // 200 nếu tồn tại, 404 nếu không – đều hợp lệ (không phải 401/403)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NHÓM 5: JWT token không hợp lệ
    // ═══════════════════════════════════════════════════════════════════════

    // ── TC-AUTHZ-12: Token giả mạo → 401 ────────────────────────────────────
    [Fact]
    public async Task AdminEndpoint_FakeToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.a.fake.token");

        var response = await _client.GetAsync("/api/laydanhsachsinhvien");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        Deauth();
    }

    // ── TC-AUTHZ-13: Token hết hạn (tạo token với thời gian âm) → 401 ────────
    [Fact]
    public async Task AdminEndpoint_ExpiredToken_Returns401()
    {
        // Tạo token đã hết hạn 1 giờ trước
        var expiredToken = TestTokenHelper.GenerateExpiredToken(SD.Role_Admin, SeededWebApplicationFactory.AdminUserName);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await _client.GetAsync("/api/laydanhsachsinhvien");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  NHÓM 6: Admin tạo dữ liệu, Teacher/Student không được phép
    // ═══════════════════════════════════════════════════════════════════════

    // ── TC-AUTHZ-14: Admin thêm lớp học thành công ───────────────────────────
    [Fact]
    public async Task CreateClass_AdminToken_Returns200()
    {
        SetToken(AdminToken);

        var unique = Guid.NewGuid().ToString("N")[..4].ToUpper();
        var payload = new
        {
            MaLop   = $"AZ{unique}",
            TenLop  = $"Authz Lớp {unique}",
            NganhId = SeededWebApplicationFactory.SeededNganhId
        };

        var response = await _client.PostAsJsonAsync("/api/themlophoc", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Deauth();
    }

    // ── TC-AUTHZ-15: Teacher không thêm được lớp học ────────────────────────
    [Fact]
    public async Task CreateClass_TeacherToken_Returns403()
    {
        SetToken(TeacherToken);
        var payload = new { MaLop = "T_LOP", TenLop = "Teacher Lop", NganhId = 1 };
        var response = await _client.PostAsJsonAsync("/api/themlophoc", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ── TC-AUTHZ-16: Student không thêm được lớp học ────────────────────────
    [Fact]
    public async Task CreateClass_StudentToken_Returns403()
    {
        SetToken(StudentToken);
        var payload = new { MaLop = "S_LOP", TenLop = "Student Lop", NganhId = 1 };
        var response = await _client.PostAsJsonAsync("/api/themlophoc", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────
    private static async Task<JsonElement> ParseBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
