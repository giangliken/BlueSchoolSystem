using BlueSchoolSystem.Models.ViewModel;
using CsvHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
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
                    PhoneNumber = "0123456789",
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, SD.Role_Admin);
                }
            }

            //Load dữ liệu mặc định cho Khoa Viện
            if (!await context.Khoas.AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "DATA", "KhoaVien.csv");
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<Khoa>().ToList();

                    context.Database.OpenConnection();
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Khoas ON");
                        context.Khoas.AddRange(records);
                        await context.SaveChangesAsync();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Khoas OFF");
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }
            }

            //Load dữ liệu mặc định cho ngành học
            if (!await context.NganhHocs.AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "DATA", "NganhHoc.csv");
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<NganhHocMap>();

                    var records = csv.GetRecords<NganhHoc>().ToList();

                    context.Database.OpenConnection();
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT NganhHocs ON");
                        context.NganhHocs.AddRange(records);
                        await context.SaveChangesAsync();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT NganhHocs OFF");
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }

            }

            //Load dữ liệu mặc định cho lớp học
            if (!await context.LopHocs.AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "DATA", "LopHoc.csv");
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<LopHocMap>();
                    var records = csv.GetRecords<LopHoc>().ToList();

                    context.Database.OpenConnection();
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT LopHocs ON");
                        context.LopHocs.AddRange(records);
                        await context.SaveChangesAsync();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT LopHocs OFF");
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }
            }

            //Load dữ liệu mặc định cho trạng thái
            if (!await context.TrangThais.AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "DATA", "TrangThai.csv");
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<TrangThai>().ToList();

                    context.Database.OpenConnection();
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT TrangThais ON");
                        context.TrangThais.AddRange(records);
                        await context.SaveChangesAsync();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT TrangThais OFF");
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }
            }

            //Load dữ liệu mặc định cho học kỳ
            if (!await context.HocKys.AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "DATA", "HocKy.csv");
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<HocKyMap>();

                    var records = csv.GetRecords<HocKy>().ToList();

            //        context.Database.OpenConnection();
            //        try
            //        {
            //            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT HocKys ON");
            //            context.HocKys.AddRange(records);
            //            await context.SaveChangesAsync();
            //            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT HocKys OFF");
            //        }
            //        finally
            //        {
            //            context.Database.CloseConnection();
            //        }
            //    }
            //}



            //Load dữ liệu cho Cơ Sở Phòng Học
            if ( !await context.CoSos.AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "DATA", "CoSo.csv");
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<CoSo>().ToList();
                    context.Database.OpenConnection();
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT CoSos ON");
                        context.CoSos.AddRange(records);
                        await context.SaveChangesAsync();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT CoSos OFF");
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }
            }


            // Load dữ liệu cho Phòng học
            if (!await context.PhongHocs.AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "DATA", "PhongHoc.csv");
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<PhongHocMap>();
                    var records = csv.GetRecords<PhongHoc>().ToList();

                    context.Database.OpenConnection();
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT PhongHocs ON");
                        context.PhongHocs.AddRange(records);
                        await context.SaveChangesAsync();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT PhongHocs OFF");
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }
            }



            //Load dữ liệu cho Môn học
            if (!await context.MonHocs.AnyAsync())
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "DATA", "MonHoc.csv");
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<MonHoc>().ToList();
                    context.Database.OpenConnection();
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT MonHocs ON");
                        context.MonHocs.AddRange(records);
                        await context.SaveChangesAsync();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT MonHocs OFF");
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }
            }

            

        }
    }
}
