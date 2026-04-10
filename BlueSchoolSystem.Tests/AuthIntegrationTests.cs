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
/// Integration tests cho chức năng đăng nhập / đăng xuất
/// Endpoint: POST /api/login
/// </summary>
[Collection("SeededFactory")]
public class AuthIntegrationTests : IClassFixture<SeededWebApplicationFactory>
{
    private readonly SeededWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(SeededWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-AUTH-01: Đăng nhập thành công với Admin
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Login_ValidAdmin_Returns200WithToken()
    {
        // Arrange
        var payload = new { UserName = SeededWebApplicationFactory.AdminUserName, Password = SeededWebApplicationFactory.AdminPassword };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", payload);
        var body     = await ParseBodyAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("code").GetInt32().Should().Be(200);
        body.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refresh_token").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("message").GetString().Should().Be("Đăng nhập thành công");

        var user = body.GetProperty("user");
        user.GetProperty("username").GetString().Should().Be(SeededWebApplicationFactory.AdminUserName);
        user.GetProperty("role").GetString().Should().Be(SD.Role_Admin);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-AUTH-02: Đăng nhập thành công với Teacher
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Login_ValidTeacher_Returns200WithTeacherInfo()
    {
        // Arrange
        var payload = new { UserName = SeededWebApplicationFactory.TeacherUserName, Password = SeededWebApplicationFactory.TeacherPassword };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", payload);
        var body     = await ParseBodyAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();
        body.GetProperty("token").GetString().Should().NotBeNullOrEmpty();

        var user = body.GetProperty("user");
        user.GetProperty("role").GetString().Should().Be(SD.Role_Teacher);
        user.GetProperty("maGV").GetString().Should().Be("GV_TEST_001");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-AUTH-03: Đăng nhập thành công với Student
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Login_ValidStudent_Returns200WithStudentInfo()
    {
        // Arrange
        var payload = new { UserName = SeededWebApplicationFactory.StudentUserName, Password = SeededWebApplicationFactory.StudentPassword };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", payload);
        var body     = await ParseBodyAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("result").GetBoolean().Should().BeTrue();

        var user = body.GetProperty("user");
        user.GetProperty("role").GetString().Should().Be(SD.Role_Student);
        user.GetProperty("mssv").GetString().Should().Be(SeededWebApplicationFactory.StudentMSSV);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-AUTH-04: Đăng nhập sai mật khẩu → 401
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        var payload = new { UserName = SeededWebApplicationFactory.AdminUserName, Password = "WrongPassword@999" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", payload);
        var body     = await ParseBodyAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("result").GetBoolean().Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-AUTH-05: Đăng nhập với tài khoản không tồn tại → 401
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Arrange
        var payload = new { UserName = "nonexistent_xyz_999", Password = "AnyPassword@1" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", payload);
        var body     = await ParseBodyAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("result").GetBoolean().Should().BeFalse();
        body.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-AUTH-06: Đăng nhập thiếu username → 400 hoặc 401
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task Login_EmptyUsername_ReturnsBadRequestOrUnauthorized()
    {
        // Arrange
        var payload = new { UserName = "", Password = SeededWebApplicationFactory.AdminPassword };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", payload);

        // Assert – server có thể trả 400 (validation) hoặc 401 (user không tìm thấy)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-AUTH-07: Truy cập endpoint được bảo vệ không có token → 401
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task AccessProtectedEndpoint_NoToken_Returns401()
    {
        // Không đặt Authorization header
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/laydanhsachsinhvien");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TC-AUTH-08: Truy cập endpoint với token hợp lệ → 200
    // ═══════════════════════════════════════════════════════════════════════
    [Fact]
    public async Task AccessProtectedEndpoint_WithValidAdminToken_Returns200()
    {
        // Arrange - đăng nhập để lấy token thật
        var loginPayload = new { UserName = SeededWebApplicationFactory.AdminUserName, Password = SeededWebApplicationFactory.AdminPassword };
        var loginResp    = await _client.PostAsJsonAsync("/api/login", loginPayload);
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await ParseBodyAsync(loginResp);
        var token     = loginBody.GetProperty("token").GetString();
        token.Should().NotBeNullOrEmpty();

        // Đặt token vào header
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/laydanhsachsinhvien");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
    }

    // ─── Helper ─────────────────────────────────────────────────────────────
    private static async Task<JsonElement> ParseBodyAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
