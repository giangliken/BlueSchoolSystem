using BlueSchoolSystem.APIControllers;
using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using BlueSchoolSystem.Repository;
using BlueSchoolSystem.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace BlueSchoolSystem.Tests;

/// <summary>
/// Unit tests for POST /api/login
/// </summary>
public class LoginControllerTests
{
    // ─── Cấu hình JWT dùng cho test ────────────────────────────────────────
    private readonly JwtSettings _jwtSettings = new()
    {
        Issuer = "blueschool",
        Audience = "blueschool_users",
        SecretKey = "4vmySLK3dpVl100gVdZsqOrurvmVvyqCnrrXVpeW"  // >= 32 ký tự
    };

    private readonly GoogleAuthSettings _googleSettings = new()
    {
        ClientId = "test-client-id",
        ClientSecret = "test-client-secret"
    };

    // ─── Helpers ────────────────────────────────────────────────────────────

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateMockSignInManager(
        Mock<UserManager<ApplicationUser>> mockUserManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(
            mockUserManager.Object, contextAccessor.Object, claimsFactory.Object,
            null, null, null, null);
    }

    private APIAccountController CreateController(
        Mock<UserManager<ApplicationUser>> mockUserManager,
        Mock<SignInManager<ApplicationUser>> mockSignInManager,
        ApplicationDbContext context)
    {
        var controller = new APIAccountController(
            mockSignInManager.Object,
            mockUserManager.Object,
            Options.Create(_jwtSettings),
            Options.Create(_googleSettings),
            new Mock<IEmailSender>().Object,
            context);

        // DefaultHttpContext với mock session để tránh lỗi "Session has not been configured"
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new MockHttpSession();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return controller;
    }

    /// <summary>Lấy giá trị property từ anonymous object qua JSON serialization</summary>
    private static JsonElement GetResponse(OkObjectResult result)
    {
        var json = JsonSerializer.Serialize(result.Value);
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement GetResponse(UnauthorizedObjectResult result)
    {
        var json = JsonSerializer.Serialize(result.Value);
        return JsonDocument.Parse(json).RootElement;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES
    // ═══════════════════════════════════════════════════════════════════════

    // ── TC01: Đăng nhập thành công với tài khoản Admin ──────────────────────
    [Fact]
    public async Task Login_ValidAdminCredentials_Returns200WithToken()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = "admin-id-001",
            UserName = "admin",
            Email = "admin@gmail.com",
            PhoneNumber = "0123456789"
        };

        var usersQuery = new TestAsyncEnumerable<ApplicationUser>(new[] { testUser });

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(usersQuery);
        mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                       .ReturnsAsync(new List<string> { SD.Role_Admin });

        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        mockSignInManager
            .Setup(m => m.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), "Admin@123", false))
            .ReturnsAsync(IdentitySignInResult.Success);

        var controller = CreateController(mockUserManager, mockSignInManager, CreateInMemoryContext());

        // Act
        var result = await controller.Login(new LoginRequest { UserName = "admin", Password = "Admin@123" });

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var body = GetResponse(ok);
        Assert.True(body.GetProperty("result").GetBoolean());
        Assert.Equal(200, body.GetProperty("code").GetInt32());
        Assert.Equal("Đăng nhập thành công", body.GetProperty("message").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("refresh_token").GetString()));

        var user = body.GetProperty("user");
        Assert.Equal("admin", user.GetProperty("username").GetString());
        Assert.Equal(SD.Role_Admin, user.GetProperty("role").GetString());
    }

    // ── TC02: Đăng nhập thành công với tài khoản Sinh viên ────────────────────
    [Fact]
    public async Task Login_ValidStudentCredentials_Returns200WithStudentInfo()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = "sv-id-001",
            UserName = "sv001",
            Email = "sv001@school.edu.vn",
            PhoneNumber = "0987654321",
            SinhViens = new SinhVien
            {
                Id = 1,
                MSSV = "2021001001",
                HoVaTenDem = "Nguyen Van",
                Ten = "A",
                CCCD = "000000000001",
                NgaySinh = new DateTime(2003, 5, 10)
            }
        };

        var usersQuery = new TestAsyncEnumerable<ApplicationUser>(new[] { testUser });

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(usersQuery);
        mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                       .ReturnsAsync(new List<string> { SD.Role_Student });

        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        mockSignInManager
            .Setup(m => m.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), "Student@123", false))
            .ReturnsAsync(IdentitySignInResult.Success);

        var controller = CreateController(mockUserManager, mockSignInManager, CreateInMemoryContext());

        // Act
        var result = await controller.Login(new LoginRequest { UserName = "sv001", Password = "Student@123" });

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var body = GetResponse(ok);
        Assert.True(body.GetProperty("result").GetBoolean());

        var user = body.GetProperty("user");
        Assert.Equal(SD.Role_Student, user.GetProperty("role").GetString());
        Assert.Equal("2021001001", user.GetProperty("mssv").GetString());
        Assert.Equal("Nguyen Van", user.GetProperty("hoSv").GetString());
        Assert.Equal("A", user.GetProperty("tenSv").GetString());
    }

    // ── TC03: Đăng nhập thành công với tài khoản Giảng viên ──────────────────
    [Fact]
    public async Task Login_ValidTeacherCredentials_Returns200WithTeacherInfo()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = "gv-id-001",
            UserName = "gv001",
            Email = "gv001@school.edu.vn",
            PhoneNumber = "0912345678",
            GiangViens = new GiangVien
            {
                Id = 1,
                MaGiangVien = "GV001",
                HoVaTenDem = "Tran Thi",
                Ten = "B",
                CCCD = "000000000002",
                NgaySinh = new DateTime(1985, 3, 20)
            }
        };

        var usersQuery = new TestAsyncEnumerable<ApplicationUser>(new[] { testUser });

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(usersQuery);
        mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                       .ReturnsAsync(new List<string> { SD.Role_Teacher });

        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        mockSignInManager
            .Setup(m => m.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), "Teacher@123", false))
            .ReturnsAsync(IdentitySignInResult.Success);

        var controller = CreateController(mockUserManager, mockSignInManager, CreateInMemoryContext());

        // Act
        var result = await controller.Login(new LoginRequest { UserName = "gv001", Password = "Teacher@123" });

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);

        var body = GetResponse(ok);
        Assert.True(body.GetProperty("result").GetBoolean());

        var user = body.GetProperty("user");
        Assert.Equal(SD.Role_Teacher, user.GetProperty("role").GetString());
        Assert.Equal("GV001", user.GetProperty("maGV").GetString());
        Assert.Equal("Tran Thi", user.GetProperty("hoGV").GetString());
        Assert.Equal("B", user.GetProperty("tenGV").GetString());
    }

    // ── TC04: Tên đăng nhập không tồn tại → 401 ─────────────────────────────
    [Fact]
    public async Task Login_UserNotFound_Returns401WithCorrectMessage()
    {
        // Arrange – danh sách user rỗng
        var usersQuery = new TestAsyncEnumerable<ApplicationUser>(Enumerable.Empty<ApplicationUser>());

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(usersQuery);

        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var controller = CreateController(mockUserManager, mockSignInManager, CreateInMemoryContext());

        // Act
        var result = await controller.Login(new LoginRequest { UserName = "khongtontai", Password = "Any@123" });

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);

        var body = GetResponse(unauthorized);
        Assert.False(body.GetProperty("result").GetBoolean());
        Assert.Equal(401, body.GetProperty("code").GetInt32());
        Assert.Equal("Tên đăng nhập không tồn tại", body.GetProperty("message").GetString());
    }

    // ── TC05: Mật khẩu sai → 401 ────────────────────────────────────────────
    [Fact]
    public async Task Login_WrongPassword_Returns401WithCorrectMessage()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = "admin-id-001",
            UserName = "admin",
            Email = "admin@gmail.com",
            PhoneNumber = "0123456789"
        };

        var usersQuery = new TestAsyncEnumerable<ApplicationUser>(new[] { testUser });

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(usersQuery);

        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        mockSignInManager
            .Setup(m => m.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), false))
            .ReturnsAsync(IdentitySignInResult.Failed);

        var controller = CreateController(mockUserManager, mockSignInManager, CreateInMemoryContext());

        // Act
        var result = await controller.Login(new LoginRequest { UserName = "admin", Password = "SaiMatKhau" });

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);

        var body = GetResponse(unauthorized);
        Assert.False(body.GetProperty("result").GetBoolean());
        Assert.Equal(401, body.GetProperty("code").GetInt32());
        Assert.Equal("Mật khẩu không đúng", body.GetProperty("message").GetString());
    }

    // ── TC06: ModelState không hợp lệ (field UserName bị thiếu) → 400 ────────
    [Fact]
    public async Task Login_MissingUserName_Returns400()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var controller = CreateController(mockUserManager, mockSignInManager, CreateInMemoryContext());

        // Giả lập validation lỗi như model binding sẽ làm
        controller.ModelState.AddModelError("UserName", "Tên đăng nhập là bắt buộc.");

        // Act
        var result = await controller.Login(new LoginRequest { UserName = null!, Password = "Admin@123" });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── TC07: ModelState không hợp lệ (field Password bị thiếu) → 400 ────────
    [Fact]
    public async Task Login_MissingPassword_Returns400()
    {
        // Arrange
        var mockUserManager = CreateMockUserManager();
        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        var controller = CreateController(mockUserManager, mockSignInManager, CreateInMemoryContext());

        controller.ModelState.AddModelError("Password", "Mật khẩu là bắt buộc.");

        // Act
        var result = await controller.Login(new LoginRequest { UserName = "admin", Password = null! });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── TC08: Response thành công chứa JWT có thể decode ────────────────────
    [Fact]
    public async Task Login_ValidCredentials_TokenIsValidJwt()
    {
        // Arrange
        var testUser = new ApplicationUser
        {
            Id = "admin-id-001",
            UserName = "admin",
            Email = "admin@gmail.com",
            PhoneNumber = "0123456789"
        };

        var usersQuery = new TestAsyncEnumerable<ApplicationUser>(new[] { testUser });

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(usersQuery);
        mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                       .ReturnsAsync(new List<string> { SD.Role_Admin });

        var mockSignInManager = CreateMockSignInManager(mockUserManager);
        mockSignInManager
            .Setup(m => m.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), "Admin@123", false))
            .ReturnsAsync(IdentitySignInResult.Success);

        var controller = CreateController(mockUserManager, mockSignInManager, CreateInMemoryContext());

        // Act
        var result = await controller.Login(new LoginRequest { UserName = "admin", Password = "Admin@123" });

        // Assert – token phải có đúng 3 phần phân cách bởi dấu '.'
        var ok = Assert.IsType<OkObjectResult>(result);
        var body = GetResponse(ok);
        var token = body.GetProperty("token").GetString();

        Assert.NotNull(token);
        Assert.Equal(3, token!.Split('.').Length); // header.payload.signature
    }
}
