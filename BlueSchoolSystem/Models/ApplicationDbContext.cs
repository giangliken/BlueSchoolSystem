using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace BlueSchoolSystem.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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
        public DbSet<LichHoc> LichHocs { get; set; } // Bảng buổi học
        public DbSet<ChiTietLopHocPhan> ChiTietLopHocPhans { get; set; } // Bảng chi tiết lớp học phần
        public DbSet<DiemDanh> DiemDanhs { get; set; } // Bảng điểm danh
        public DbSet<ChiTietDiemDanh> ChiTietDiemDanhs { get; set; } // Bảng chi tiết điểm danh
        public DbSet<PhongHoc> PhongHocs { get; set; } // Bảng phòng học
        public DbSet<CoSo> CoSos { get; set; } // Bảng cơ sở
        public DbSet<BangDiem> BangDiems { get; set; } // Bảng điểm
        public DbSet<DangKyHocPhan> DangKyHocPhans { get; set; } // Bảng đăng ký học phần
        public DbSet<HocKy> HocKys { get; set; } // Bảng học kỳ
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<UserFaceTemplate> UserFaceTemplates { get; set; }
        public DbSet<FaceVerifyLog> FaceVerifyLogs { get; set; }
        public DbSet<TrangThai> TrangThais { get; set; } // Bảng trạng thái của hệ thống
        public DbSet<KhoaHoc> KhoaHocs { get; set; } // Bảng khóa học
        public DbSet<ChuongTrinhDaoTao> ChuongTrinhDaoTaos { get; set; } // Bảng chương trình đào tạo
        public DbSet<ChiTietChuongTrinhDaoTao> ChiTietChuongTrinhDaoTaos { get; set; } // Bảng chi tiết chương trình đào tạo
        public DbSet<DotDangKy> DotDangKys { get; set; } // Bảng đợt đăng ký
        public DbSet<GiangVienMonHoc> GiangVienMonHocs { get; set; }
        public DbSet<DinhMucHocPhi> DinhMucHocPhis { get; set; }
        public DbSet<HocPhi> HocPhis { get; set; }
        public DbSet<ChiTietHocPhi> ChiTietHocPhis { get; set; }
        public DbSet<PhieuThu> PhieuThus { get; set; }
        public DbSet<SuKien> SuKiens { get; set; } // Bảng sự kiện


        public DbSet<XinVangDay> XinVangDays { get; set; } // Bảng xin vắng dạy

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

            builder.Entity<PhongHoc>()
                .HasOne(ph => ph.CoSo)
                .WithMany(cs => cs.PhongHocs)
                .HasForeignKey(ph => ph.CoSoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChiTietDiemDanh>()
                .HasOne(ct => ct.TrangThai)
                .WithMany()
                .HasForeignKey(ct => ct.TrangThaiId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChiTietDiemDanh>()
                .HasOne(ct => ct.SinhVien)
                .WithMany(sv => sv.ChiTietDiemDanhs)
                .HasForeignKey(ct => ct.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChiTietDiemDanh>()
                .HasOne(ct => ct.DiemDanh)
                .WithMany(dd => dd.ChiTietDiemDanhs)
                .HasForeignKey(ct => ct.DiemDanhId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DiemDanh>()
                .HasOne(dd => dd.TrangThai)
                .WithMany()
                .HasForeignKey(dd => dd.TrangThaiId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LichThi>()
                   .HasOne(l => l.TrangThai)
                   .WithMany()
                   .HasForeignKey(l => l.TrangThaiId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LopHocPhan>()
                .HasOne(l => l.TrangThai)
                .WithMany()
                .HasForeignKey(l => l.TrangThaiId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DangKyHocPhan>()
                .HasOne(dk => dk.SinhVien)
                .WithMany() 
                .HasForeignKey(dk => dk.SinhVienId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<DangKyHocPhan>()
                .HasOne(dk => dk.LopHocPhan)
                .WithMany() 
                .HasForeignKey(dk => dk.LopHocPhanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GiangVienMonHoc>()
        .HasKey(x => new { x.GiangVienId, x.MonHocId });

            builder.Entity<GiangVienMonHoc>()
                .HasOne(x => x.GiangVien)
                .WithMany(x => x.GiangVienMonHocs)
                .HasForeignKey(x => x.GiangVienId);

            builder.Entity<GiangVienMonHoc>()
                .HasOne(x => x.MonHoc)
                .WithMany(x => x.GiangVienMonHocs)
                .HasForeignKey(x => x.MonHocId);

            builder.Entity<UserFaceTemplate>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Embedding).HasColumnType("varbinary(max)");
                e.Property(x => x.Model).HasMaxLength(100);
                e.HasOne(x => x.User)
                    .WithMany() 
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.UserId, x.IsActive });
                e.ToTable("UserFaceTemplates");
            });

            //  Cấu hình FaceVerifyLog (audit)
            builder.Entity<FaceVerifyLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.UserId, x.At });
                e.Property(x => x.Score).HasColumnType("real"); // float32
                e.ToTable("FaceVerifyLogs");
            });

            builder.Entity<XinVangDay>(e =>
            {
                e.HasKey(x => x.Id);

                // GiangVien: cascade delete
                e.HasOne(x => x.GiangVien)
                    .WithMany()
                    .HasForeignKey(x => x.GiangVienId)
                    .OnDelete(DeleteBehavior.Cascade);

                // LichHoc: không cascade
                e.HasOne(x => x.LichHoc)
                    .WithMany()
                    .HasForeignKey(x => x.LichHocId)
                    .OnDelete(DeleteBehavior.Restrict);

                // LopHocPhan: không cascade
                e.HasOne(x => x.LopHocPhan)
                    .WithMany()
                    .HasForeignKey(x => x.LopHocPhanId)
                    .OnDelete(DeleteBehavior.Restrict);

                // TrangThai: không cascade
                e.HasOne(x => x.TrangThai)
                    .WithMany()
                    .HasForeignKey(x => x.TrangThaiId)
                    .OnDelete(DeleteBehavior.Restrict);


                e.HasOne(x => x.LichHoc)
                .WithMany() // hoặc WithOne nếu 1:1
                .HasForeignKey(x => x.LichHocId)
                .OnDelete(DeleteBehavior.Restrict); // KHÔNG cascade

                e.Property(x => x.LyDo).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
                e.ToTable("XinVangDays");
            });


            builder.Entity<HocPhi>(e =>
            {
                e.HasKey(h => h.Id);
                e.Property(h => h.DuNoConLai).HasColumnType("decimal(18, 2)");

                // Quan hệ 1-1 với SinhVien
                e.HasOne(h => h.SinhVien)
                 .WithOne() // Bên SV không cần navigation property ngược lại (hoặc thêm nếu muốn)
                 .HasForeignKey<HocPhi>(h => h.SinhVienId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ChiTietHocPhi
            builder.Entity<ChiTietHocPhi>(e =>
            {
                e.HasKey(ct => ct.Id);
                e.Property(ct => ct.SoTien).HasColumnType("decimal(18, 2)");

                // Quan hệ N-1 với HocPhi
                e.HasOne(ct => ct.HocPhi)
                 .WithMany() // HocPhi không cần list chi tiết nếu không dùng
                 .HasForeignKey(ct => ct.HocPhiId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ 1-1 (hoặc N-1) với DangKyHocPhan
                e.HasOne(ct => ct.DangKyHocPhan)
                 .WithMany()
                 .HasForeignKey(ct => ct.DangKyHocPhanId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // PhieuThu
            builder.Entity<PhieuThu>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.SoTienDong).HasColumnType("decimal(18, 2)");

                // Quan hệ N-1 với SinhVien (để biết ai đóng)
                // Lưu ý: Không cần quan hệ với TaiKhoanSinhVien nữa vì bảng đó đã xóa
                e.HasOne<SinhVien>() // Chỉ định rõ kiểu nếu không có navigation property
                 .WithMany()
                 .HasForeignKey(p => p.SinhVienId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // DinhMucHocPhi
            builder.Entity<DinhMucHocPhi>(e =>
            {
                e.Property(d => d.GiaTienMotTinChi).HasColumnType("decimal(18, 2)");
                e.HasOne(d => d.NganhHoc)
                 .WithMany()
                 .HasForeignKey(d => d.NganhId)
                 .OnDelete(DeleteBehavior.Restrict);
            });




        }
    }
}
