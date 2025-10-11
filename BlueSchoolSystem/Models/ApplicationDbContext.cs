using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace BlueSchoolSystem.Models
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        { 
        }

        //bảng lưu trữ hoạt động người dùng
        
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetOTP> PasswordResetOTPs { get; set; } // Bảng lưu trữ mã OTP để đặt lại mật khẩu
        public DbSet<SinhVien> SinhViens { get; set; } // Bảng sinh viên
        public DbSet<LopHoc> LopHocs { get; set; } // Bảng lớp học
        public DbSet<ChiTietLopHoc> ChiTietLopHocs { get; set; } // Bảng chi tiết lớp học
        public DbSet<Khoa> Khoas { get; set; } // Bảng khoa
        public DbSet<ChiTietKhoaVien> ChiTietKhoaViens { get; set; } // Bảng chi tiết khoa viện
        public DbSet<NganhHoc> NganhHocs { get; set; } // Bảng ngành học
        public DbSet<MonHoc> MonHocs { get; set; } // Bảng môn học
        public DbSet<GiangVien> GiangViens { get; set; } // Bảng giảng viên
        public DbSet<LopHocPhan> LopHocPhans { get; set; } // Bảng lớp học phần
        public DbSet<LichThi> LichThis { get; set; } // Bảng lịch thi
        public DbSet<ChiTietLopHocPhan> ChiTietLopHocPhans { get; set; } // Bảng chi tiết lớp học phần
        public DbSet<DiemDanh> DiemDanhs { get; set; } // Bảng điểm danh
        public DbSet<PhongHoc> PhongHocs { get; set; } // Bảng phòng học
        public DbSet<BangDiem> BangDiems { get; set; } // Bảng điểm
        public DbSet<DangKyHocPhan> DangKyHocPhans { get; set; } // Bảng đăng ký học phần
        public DbSet<HocKy> HocKys { get; set; } // Bảng học kỳ

        //Table lưu trạng thái của hệ thống
        public DbSet<TrangThai> TrangThais { get; set; } // Bảng trạng thái của hệ thống
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.SinhViens)
                .WithOne(sv => sv.User)
                .HasForeignKey<SinhVien>(sv => sv.UserId);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.GiangViens)
                .WithOne(gv => gv.User)
                .HasForeignKey<GiangVien>(gv => gv.UserId);

            builder.Entity<BangDiem>()
                .HasOne(bd => bd.SinhVien)
                .WithMany(sv => sv.BangDiems)
                .HasForeignKey(bd => bd.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<BangDiem>()
                .HasOne(bd => bd.LopHocPhan)
                .WithMany(lhp => lhp.BangDiems)
                .HasForeignKey(bd => bd.LopHocPhanId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<DiemDanh>()
                .HasOne(dd => dd.TrangThai)
                .WithMany()
                .HasForeignKey(dd => dd.TrangThaiId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DiemDanh>()
                .HasOne(dd => dd.ChiTietLopHocPhan)
                .WithMany(ct => ct.DiemDanhs)   
                .HasForeignKey(dd => dd.ChiTietLopHocPhanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<LichThi>()
                   .HasOne(l => l.TrangThai)
                   .WithMany()
                   .HasForeignKey(l => l.TrangThaiId)
                   .OnDelete(DeleteBehavior.Restrict); 

        }
    }
}
