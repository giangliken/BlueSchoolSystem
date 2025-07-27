using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlueSchoolSystem.Models
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        { 
        }

        public DbSet<SinhVien> SinhViens { get; set; } // Bảng sinh viên
        public DbSet<LopHoc> LopHocs { get; set; } // Bảng lớp học
        public DbSet<Khoa> Khoas { get; set; } // Bảng khoa
        public DbSet<NganhHoc> NganhHocs { get; set; } // Bảng ngành học
        //public DbSet<MonHoc> MonHocs { get; set; } // Bảng môn học
        //public DbSet<GiangVien> GiangViens { get; set; } // Bảng giảng viên
        //public DbSet<LopHocPhan> LopHocPhans { get; set; } // Bảng lớp học phần
        //public DbSet<BangDiem> BangDiems { get; set; } // Bảng điểm
        //public DbSet<DangKyHocPhan> DangKyHocPhans { get; set; } // Bảng đăng ký học phần
        //public DbSet<LichGiangDay> LichGiangDays { get; set; } // Bảng lịch giảng dạy
        //public DbSet<LichHoc> LichHocs { get; set; } // Bảng lịch học

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
        }




    }
}
