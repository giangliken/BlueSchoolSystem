using BlueSchoolSystem.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlueSchoolSystem.Tests;

/// <summary>
/// Integration tests cho CRUD Môn Học
/// GET    /api/laydanhsachmonhoc         – Admin
/// GET    /api/monhoc/{id}               – Public
/// GET    /api/monhocchitiet/{id}        – Admin
/// POST   /api/taomonhoc                – Admin
/// PUT    /api/suamonhoc/{id}           – Admin
/// DELETE /api/xoamonhoc/{id}           – Admin
/// </summary>
public class SubjectIntegrationTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly SeededWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SubjectIntegrationTests(SeededWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── Auth helpers ─────────────────────────────────────────────────────────
    private void AuthAsAdmin() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Admin, SeededWebApplicationFactory.AdminUserName));

    private void AuthAsTeacher() =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokenHelper.GenerateToken(SD.Role_Teacher, SeededWebApplicationFactory.TeacherUserName));

    private void Deauth() => _client.DefaultRequestHeaders.Authorization = null;

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-01: Lấy danh sách môn học (Admin) → 200 + có dữ liệu
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetAllSubjects_AdminToken_Returns200WithData()
    {
        AuthAsAdmin();

        var response = await _client.GetAsync("/api/laydanhsachmonhoc");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("code").GetInt32().Should().Be(200);
        body.GetProperty("tongmonhoc").GetInt32().Should().BeGreaterThanOrEqualTo(2);

        var data = body.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-02: Lấy danh sách môn học không có token → 401
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetAllSubjects_NoToken_Returns401()
    {
        Deauth();
        var response = await _client.GetAsync("/api/laydanhsachmonhoc");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-03: Lấy thông tin môn học theo Id hợp lệ (public) → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetSubjectById_ValidId_Returns200()
    {
        Deauth();

        var response = await _client.GetAsync($"/api/monhoc/{SeededWebApplicationFactory.SeededMonHocId}");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("code").GetInt32().Should().Be(200);

        var data = body.GetProperty("data");
        data.GetProperty("maMonHoc").GetString().Should().Be(SeededWebApplicationFactory.SeededMaMonHoc);
        data.GetProperty("soTinChi").GetInt32().Should().Be(3);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-04: Lấy môn học theo Id không tồn tại → 404
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetSubjectById_InvalidId_Returns404()
    {
        Deauth();

        var response = await _client.GetAsync("/api/monhoc/99999");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("result").GetBoolean().Should().BeFalse();
        body.GetProperty("code").GetInt32().Should().Be(404);
        body.GetProperty("message").GetString().Should().Contain("Không tìm thấy");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-05: Lấy chi tiết môn học (Admin) → 200 + bao gồm ngành, giảng viên
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task GetSubjectDetail_AdminToken_Returns200WithRelations()
    {
        AuthAsAdmin();

        var response = await _client.GetAsync($"/api/monhocchitiet/{SeededWebApplicationFactory.SeededMonHocId}");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();

        var data = body.GetProperty("data");
        data.GetProperty("maMonHoc").GetString().Should().Be(SeededWebApplicationFactory.SeededMaMonHoc);
        data.TryGetProperty("nganhs", out _).Should().BeTrue();
        data.TryGetProperty("giangViens", out _).Should().BeTrue();

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-06: Tạo môn học mới thành công → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateSubject_ValidData_Returns200WithNewSubject()
    {
        AuthAsAdmin();

        var unique  = Guid.NewGuid().ToString("N")[..6].ToUpper();
        var payload = new
        {
            MaMonHoc  = $"MH_{unique}",
            TenMonHoc = $"Môn học test {unique}",
            SoTinChi  = 3,
            MoTa      = "Mô tả integration test"
        };

        var response = await _client.PostAsJsonAsync("/api/taomonhoc", payload);
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("code").GetInt32().Should().Be(200);
        body.GetProperty("message").GetString().Should().Contain("thành công");

        var data = body.GetProperty("data");
        data.GetProperty("maMonHoc").GetString().Should().Be(payload.MaMonHoc);
        data.GetProperty("soTinChi").GetInt32().Should().Be(3);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-07: Tạo môn học với role Teacher → 403
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task CreateSubject_TeacherToken_Returns403()
    {
        AuthAsTeacher();

        var payload = new { MaMonHoc = "MH_T", TenMonHoc = "Test Subject", SoTinChi = 2 };
        var response = await _client.PostAsJsonAsync("/api/taomonhoc", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-08: Cập nhật môn học hợp lệ → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task UpdateSubject_ValidData_Returns200()
    {
        // Tạo một môn học mới để cập nhật (tránh side-effect với môn seed)
        AuthAsAdmin();

        var uniqueCode = $"MH_UPD_{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
        var createPayload = new { MaMonHoc = uniqueCode, TenMonHoc = "Before update", SoTinChi = 2 };
        var createResp    = await _client.PostAsJsonAsync("/api/taomonhoc", createPayload);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var createBody = await ParseBodyAsync(createResp);
        var newId      = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Act – Update
        var updatePayload = new
        {
            MaMonHoc  = uniqueCode,
            TenMonHoc = "After update",
            SoTinChi  = 4,
            MoTa      = "Updated description"
        };
        var updateResp = await _client.PutAsJsonAsync($"/api/suamonhoc/{newId}", updatePayload);
        var updateBody = await ParseBodyAsync(updateResp);

        // Assert
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        updateBody.GetProperty("result").GetBoolean().Should().BeTrue();
        updateBody.GetProperty("message").GetString().Should().Contain("thành công");

        var updated = updateBody.GetProperty("data");
        updated.GetProperty("tenMonHoc").GetString().Should().Be("After update");
        updated.GetProperty("soTinChi").GetInt32().Should().Be(4);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-09: Cập nhật môn học không tồn tại → 404
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task UpdateSubject_NotFound_Returns404()
    {
        AuthAsAdmin();

        var payload  = new { MaMonHoc = "GHOST", TenMonHoc = "Ghost Subject", SoTinChi = 1 };
        var response = await _client.PutAsJsonAsync("/api/suamonhoc/99999", payload);
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("result").GetBoolean().Should().BeFalse();
        body.GetProperty("code").GetInt32().Should().Be(404);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-10: Xóa môn học hợp lệ (không có quan hệ) → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task DeleteSubject_ValidId_Returns200()
    {
        AuthAsAdmin();

        // Tạo môn học mới để xóa
        var uniqueCode    = $"MH_DEL_{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
        var createPayload = new { MaMonHoc = uniqueCode, TenMonHoc = "To be deleted", SoTinChi = 1 };
        var createResp    = await _client.PostAsJsonAsync("/api/taomonhoc", createPayload);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var createBody = await ParseBodyAsync(createResp);
        var newId      = createBody.GetProperty("data").GetProperty("id").GetInt32();

        // Act – Delete
        var deleteResp = await _client.DeleteAsync($"/api/xoamonhoc/{newId}");
        var deleteBody = await ParseBodyAsync(deleteResp);

        // Assert
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteBody.GetProperty("result").GetBoolean().Should().BeTrue();
        deleteBody.GetProperty("message").GetString().Should().Contain("thành công");

        // Verify – môn không còn tồn tại
        var getResp = await _client.GetAsync($"/api/monhoc/{newId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-11: Xóa môn học không tồn tại → 404
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task DeleteSubject_NotFound_Returns404()
    {
        AuthAsAdmin();

        var response = await _client.DeleteAsync("/api/xoamonhoc/99999");
        var body     = await ParseBodyAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("result").GetBoolean().Should().BeFalse();
        body.GetProperty("code").GetInt32().Should().Be(404);

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-12: Xóa môn học đang được sử dụng (có GiangVienMonHoc) → 400
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task DeleteSubject_InUse_Returns400()
    {
        // Seed a GiangVienMonHoc reference trước
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Lấy GiangVien seeded
        var gv = db.GiangViens.FirstOrDefault();
        if (gv == null) return; // bỏ qua nếu chưa có GV

        // Tạo môn mới và gán
        var mon = new MonHoc { MaMonHoc = $"MH_INR_{Guid.NewGuid().ToString("N")[..4]}", TenMonHoc = "In Use Subject", SoTinChi = 2 };
        db.MonHocs.Add(mon);
        await db.SaveChangesAsync();

        db.GiangVienMonHocs.Add(new GiangVienMonHoc { GiangVienId = gv.Id, MonHocId = mon.Id });
        await db.SaveChangesAsync();

        // Act
        AuthAsAdmin();
        var response = await _client.DeleteAsync($"/api/xoamonhoc/{mon.Id}");
        var body     = await ParseBodyAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("result").GetBoolean().Should().BeFalse();
        body.GetProperty("code").GetInt32().Should().Be(400);
        body.GetProperty("message").GetString().Should().Contain("đang được sử dụng");

        Deauth();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-MH-13: Xóa môn học với role Teacher → 403
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task DeleteSubject_TeacherToken_Returns403()
    {
        AuthAsTeacher();

        var response = await _client.DeleteAsync($"/api/xoamonhoc/{SeededWebApplicationFactory.SeededMonHocId}");
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
