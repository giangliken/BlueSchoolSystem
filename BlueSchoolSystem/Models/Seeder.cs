using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BlueSchoolSystem.Models
{
    public static class Seeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            //Tạo role nếu chưa tồn tại
            foreach (var roleName in SD.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Tạo tài khoản admin mặc định
            string adminEmail = "admin@gmail.com";
            string adminPassword = "Admin@123";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, SD.Role_Admin);
                }
            }

            //Load dữ liệu mặc định cho khoa học
            if(!await context.Khoas.AnyAsync())
            {
                context.Khoas.AddRange(
                    new Khoa { MaKhoa = "CNTT", TenKhoa = "Khoa Công nghệ thông tin" },
                    new Khoa { MaKhoa = "TCTM", TenKhoa = "Khoa Tài chính - Thương mại" },
                    new Khoa { MaKhoa = "KTMT", TenKhoa = "Khoa Kiến trúc - Mỹ thuật" },
                    new Khoa { MaKhoa = "QTKD", TenKhoa = "Khoa Quản trị Kinh doanh" },
                    new Khoa { MaKhoa = "QTDL", TenKhoa = "Khoa QT Du lịch - Nhà hàng - Khách sạn" },
                    new Khoa { MaKhoa = "TA", TenKhoa = "Khoa Tiếng Anh" },
                    new Khoa { MaKhoa = "NBH", TenKhoa = "Khoa Nhật Bản học" },
                    new Khoa { MaKhoa = "XD", TenKhoa = "Khoa Xây dựng" },
                    new Khoa { MaKhoa = "LUAT", TenKhoa = "Khoa Luật" },
                    new Khoa { MaKhoa = "DUOC", TenKhoa = "Khoa Dược" },
                    new Khoa { MaKhoa = "HTTTQL", TenKhoa = "Khoa Hệ thống thông tin quản lý" },
                    new Khoa { MaKhoa = "TTTK", TenKhoa = "Khoa Truyền thông - Thiết kế" },
                    new Khoa { MaKhoa = "KTHUTECH", TenKhoa = "Viện Kỹ thuật HUTECH" },
                    new Khoa { MaKhoa = "KHUDHUTECH", TenKhoa = "Viện Khoa học ứng dụng HUTECH" },
                    new Khoa { MaKhoa = "KHXHNV", TenKhoa = "Viện Khoa học Xã hội và Nhân văn" },
                    new Khoa { MaKhoa = "DTQT", TenKhoa = "Viện Đào tạo Quốc tế HUTECH" },
                    new Khoa { MaKhoa = "CNVN", TenKhoa = "Viện Công nghệ Việt - Nhật" },
                    new Khoa { MaKhoa = "CNHAN", TenKhoa = "Viện Công nghệ Việt - Hàn" },
                    new Khoa { MaKhoa = "TTHNNKN", TenKhoa = "Trung tâm Tin học - Ngoại ngữ - Kỹ năng" },
                    new Khoa { MaKhoa = "GDCTQP", TenKhoa = "TT Giáo dục chính trị - Quốc phòng" },
                    new Khoa { MaKhoa = "DTXA", TenKhoa = "Trung tâm Đào tạo từ xa" },
                    new Khoa { MaKhoa = "VPDDT", TenKhoa = "Văn phòng Đảng - Đoàn thể" }
                );
                await context.SaveChangesAsync();
            }

            //Load dữ liệu mặc định cho ngành học
            if (!await context.NganhHocs.AnyAsync())
            {
                context.NganhHocs.AddRange(
                    new NganhHoc { MaNganh = "7480201", TenNganh = "Công nghệ thông tin", KhoaId = 1 },
                    new NganhHoc { MaNganh = "7480202", TenNganh = "An toàn thông tin", KhoaId = 1 },
                    new NganhHoc { MaNganh = "7480101", TenNganh = "Khoa học máy tính", KhoaId = 1 },
                    new NganhHoc { MaNganh = "7480107", TenNganh = "Trí tuệ nhân tạo", KhoaId = 1 },
                    new NganhHoc { MaNganh = "7460108", TenNganh = "Khoa học dữ liệu (Data Science)", KhoaId = 1 },
                    new NganhHoc { MaNganh = "7340405", TenNganh = "Hệ thống thông tin quản lý", KhoaId = 11 },
                    new NganhHoc { MaNganh = "7510209", TenNganh = "Robot & trí tuệ nhân tạo", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7510205", TenNganh = "Công nghệ kỹ thuật ô tô", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7520141", TenNganh = "Công nghệ ô tô điện", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7480106", TenNganh = "Kỹ thuật máy tính", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7510206", TenNganh = "Kỹ thuật nhiệt", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7520103", TenNganh = "Kỹ thuật cơ khí", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7520114", TenNganh = "Kỹ thuật cơ điện tử", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7520201", TenNganh = "Kỹ thuật điện", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7520207", TenNganh = "Kỹ thuật điện tử - viễn thông", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7520216", TenNganh = "Kỹ thuật điều khiển và tự động hóa", KhoaId = 13 },
                    new NganhHoc { MaNganh = "7580201", TenNganh = "Kỹ thuật xây dựng", KhoaId = 8 },
                    new NganhHoc { MaNganh = "7580302", TenNganh = "Quản lý xây dựng", KhoaId = 8 },
                    new NganhHoc { MaNganh = "7340201", TenNganh = "Tài chính - Ngân hàng", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7340301", TenNganh = "Kế toán", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7340205", TenNganh = "Công nghệ tài chính", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7340101", TenNganh = "Quản trị kinh doanh", KhoaId = 4 },
                    new NganhHoc { MaNganh = "7340114", TenNganh = "Digital Marketing", KhoaId = 4 },
                    new NganhHoc { MaNganh = "7340115", TenNganh = "Marketing", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7310109", TenNganh = "Kinh tế số", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7340121", TenNganh = "Kinh doanh thương mại", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7340122", TenNganh = "Thương mại điện tử", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7340120", TenNganh = "Kinh doanh quốc tế", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7310106", TenNganh = "Kinh tế quốc tế", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7340116", TenNganh = "Bất động sản", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7510605", TenNganh = "Logistics & quản lý chuỗi cung ứng", KhoaId = 2 },
                    new NganhHoc { MaNganh = "7310401", TenNganh = "Tâm lý học", KhoaId = 15 },
                    new NganhHoc { MaNganh = "7320108", TenNganh = "Quan hệ công chúng", KhoaId = 4 },
                    new NganhHoc { MaNganh = "7340404", TenNganh = "Quản trị nhân lực", KhoaId = 4 },
                    new NganhHoc { MaNganh = "7810201", TenNganh = "Quản trị khách sạn", KhoaId = 5 }

                );
                await context.SaveChangesAsync();
            }

            //Load dữ liệu mặc định cho lớp học
            if (!await context.LopHocs.AnyAsync())
            {
                context.LopHocs.AddRange(
                    new LopHoc { MaLop = "22DTHG1", TenLop = "22DTHG1", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG2", TenLop = "22DTHG2", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG3", TenLop = "22DTHG3", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG4", TenLop = "22DTHG4", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG5", TenLop = "22DTHG5", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG6", TenLop = "22DTHG6", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG7", TenLop = "22DTHG7", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG8", TenLop = "22DTHG8", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG9", TenLop = "22DTHG9", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG10", TenLop = "22DTHG10", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG11", TenLop = "22DTHG11", NganhId = 1 },
                    new LopHoc { MaLop = "22DTHG12", TenLop = "22DTHG12", NganhId = 1 }

                );
                await context.SaveChangesAsync();
            }


        }
    }
}
