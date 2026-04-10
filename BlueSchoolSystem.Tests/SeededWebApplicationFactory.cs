using BlueSchoolSystem.Models;
using BlueSchoolSystem.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace BlueSchoolSystem.Tests;

/// <summary>
/// Factory kế thừa từ CustomWebApplicationFactory, tự động seed dữ liệu test
/// (roles, users, khoa, ngành, lớp, môn học, sinh viên) vào in-memory DB trước khi chạy test.
/// </summary>
public class SeededWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // ── Tên DB in-memory duy nhất cho mỗi instance factory ──────────────────
    private readonly string _dbName = $"SeededTestDb_{Guid.NewGuid()}";

    // ── Thông tin tài khoản test dùng chung ──────────────────────────────────
    public const string AdminUserName     = "admin";
    public const string AdminPassword     = "Admin@123";
    public const string TeacherUserName   = "teacher_test";
    public const string TeacherPassword   = "Teacher@Test123!";
    public const string StudentUserName   = "2280600761";
    public const string StudentPassword   = "Abc@123";
    public const string StudentMSSV       = "2280602832";

    // ── Dữ liệu seeded để test có thể tham chiếu ────────────────────────────
    public const int    SeededKhoaId      = 1;
    public const string SeededMaKhoa      = "CNTT";
    public const int    SeededNganhId     = 1;
    public const string SeededMaNganh     = "KTPM";
    public const int    SeededLopId       = 1;
    public const string SeededMaLop       = "22KTPM1";
    public const int    SeededMonHocId    = 1;
    public const string SeededMaMonHoc    = "PTTKHT";
    public const int    SeededMonHocId2   = 2;
    public const string SeededMaMonHoc2   = "LTNET";

    // ── Cấu hình WebHost ────────────────────────────────────────────────────
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Xóa DbContextOptions<ApplicationDbContext> (SqlServer)
            var optDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (optDesc != null) services.Remove(optDesc);

            // Xóa IDbContextOptionsConfiguration<ApplicationDbContext>
            // (lưu action UseSqlServer - nếu giữ lại sẽ được áp dụng cùng InMemory)
            var cfgDescriptors = services
                .Where(d => d.ServiceType.IsGenericType
                         && d.ServiceType.GetGenericArguments().Length == 1
                         && d.ServiceType.GetGenericArguments()[0] == typeof(ApplicationDbContext)
                         && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))
                .ToList();
            foreach (var d in cfgDescriptors) services.Remove(d);

            // Xóa IDatabaseProvider (SqlServer provider)
            var provDesc = services
                .Where(d => d.ServiceType.FullName?.EndsWith(".IDatabaseProvider") == true)
                .ToList();
            foreach (var d in provDesc) services.Remove(d);

            // Thêm ApplicationDbContext dùng in-memory DB
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        // Use "Testing" so Program.cs skips Seeder.SeedAsync
        builder.UseEnvironment("Testing");
    }

    // ── Seed dữ liệu sau khi app đã khởi động ──────────────────────────────
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var sp          = scope.ServiceProvider;
        var db          = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        // 1. Tạo roles
        foreach (var role in SD.AllRoles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // 2. Seed TrangThai
        if (!db.TrangThais.Any())
        {
            db.TrangThais.AddRange(
                new TrangThai { Id = 1, TenTrangThai = "Đang học",      MoTa = "", LoaiTrangThai = "SinhVien"  },
                new TrangThai { Id = 2, TenTrangThai = "Tốt nghiệp",    MoTa = "", LoaiTrangThai = "SinhVien"  },
                new TrangThai { Id = 3, TenTrangThai = "Đang hoạt động",MoTa = "", LoaiTrangThai = "GiangVien" }
            );
            await db.SaveChangesAsync();
        }

        // 3. Seed Khoa
        if (!db.Khoas.Any())
        {
            db.Khoas.Add(new Khoa { Id = SeededKhoaId, MaKhoa = SeededMaKhoa, TenKhoa = "Công nghệ thông tin" });
            await db.SaveChangesAsync();
        }

        // 4. Seed NganhHoc
        if (!db.NganhHocs.Any())
        {
            db.NganhHocs.Add(new NganhHoc
            {
                Id = SeededNganhId, MaNganh = SeededMaNganh, TenNganh = "Kỹ thuật phần mềm",
                KhoaId = SeededKhoaId
            });
            await db.SaveChangesAsync();
        }

        // 5. Seed LopHoc
        if (!db.LopHocs.Any())
        {
            db.LopHocs.Add(new LopHoc
            {
                Id = SeededLopId, MaLop = SeededMaLop, TenLop = "Lớp KTPM K22-1",
                NganhId = SeededNganhId
            });
            await db.SaveChangesAsync();
        }

        // 6. Seed MonHoc
        if (!db.MonHocs.Any())
        {
            db.MonHocs.AddRange(
                new MonHoc { Id = SeededMonHocId,  MaMonHoc = SeededMaMonHoc,  TenMonHoc = "Phân tích thiết kế hệ thống", SoTinChi = 3 },
                new MonHoc { Id = SeededMonHocId2, MaMonHoc = SeededMaMonHoc2, TenMonHoc = "Lập trình .NET",               SoTinChi = 4 }
            );
            await db.SaveChangesAsync();
        }

        // 7. Seed Admin user
        if (await userManager.FindByNameAsync(AdminUserName) == null)
        {
            var admin = new ApplicationUser { UserName = AdminUserName, Email = "admin_test@test.com", PhoneNumber = "0901000001" };
            var r = await userManager.CreateAsync(admin, AdminPassword);
            if (r.Succeeded)
                await userManager.AddToRoleAsync(admin, SD.Role_Admin);
        }

        // 8. Seed Teacher user + GiangVien
        if (await userManager.FindByNameAsync(TeacherUserName) == null)
        {
            var teacher = new ApplicationUser { UserName = TeacherUserName, Email = "teacher_test@test.com", PhoneNumber = "0901000002" };
            var r = await userManager.CreateAsync(teacher, TeacherPassword);
            if (r.Succeeded)
            {
                await userManager.AddToRoleAsync(teacher, SD.Role_Teacher);
                var gvUser = await userManager.FindByNameAsync(TeacherUserName);
                db.GiangViens.Add(new GiangVien
                {
                    MaGiangVien = "GV_TEST_001",
                    HoVaTenDem  = "Nguyen Van",
                    Ten         = "Test",
                    CCCD        = "123456789012",
                    NgaySinh    = new DateTime(1985, 1, 1),
                    DiaChi      = "123 Test Street",
                    KhoaId      = SeededKhoaId,
                    TrangThaiId = 3,
                    UserId      = gvUser!.Id
                });
                await db.SaveChangesAsync();
            }
        }

        // 9. Seed Student user + SinhVien
        if (await userManager.FindByNameAsync(StudentUserName) == null)
        {
            var student = new ApplicationUser { UserName = StudentUserName, Email = "student_test@test.com", PhoneNumber = "0901000003" };
            var r = await userManager.CreateAsync(student, StudentPassword);
            if (r.Succeeded)
            {
                await userManager.AddToRoleAsync(student, SD.Role_Student);
                var svUser = await userManager.FindByNameAsync(StudentUserName);
                db.SinhViens.Add(new SinhVien
                {
                    MSSV          = StudentMSSV,
                    HoVaTenDem    = "Test Student",
                    Ten           = "One",
                    CCCD          = "000000000001",
                    NgaySinh      = new DateTime(2003, 1, 1),
                    GioiTinh      = true,
                    DiaChi        = "456 Test Street",
                    LopId         = SeededLopId,
                    NgayNhapHoc   = new DateTime(2021, 9, 1),
                    NgayTotNghiep = new DateTime(2025, 6, 1),
                    TrangThaiId   = 1,
                    UserId        = svUser!.Id
                });
                await db.SaveChangesAsync();
            }
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
