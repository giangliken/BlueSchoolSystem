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
        public DbSet<HoaDonHocPhi> HoaDonHocPhis { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<TaiKhoanSinhVien> TaiKhoanSinhViens { get; set; } 
        public DbSet<GiaoDichThanhToan> GiaoDichThanhToans { get; set; }
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


            // 1. HoaDonHocPhi
            builder.Entity<HoaDonHocPhi>(e =>
            {
                // Liên kết SinhVien (1-N)
                e.HasOne(h => h.SinhVien)
                 .WithMany() // Nếu trong SinhVien chưa có List<HoaDon>, để trống
                 .HasForeignKey(h => h.SinhVienId)
                 .OnDelete(DeleteBehavior.Restrict); // Không xóa SV nếu có hóa đơn

                // Decimal precision
                e.Property(h => h.TongTien).HasColumnType("decimal(18, 2)");
                e.Property(h => h.DaDong).HasColumnType("decimal(18, 2)");
                e.Property(h => h.ConLai).HasColumnType("decimal(18, 2)");
            });

            // 2. ChiTietHoaDon
            builder.Entity<ChiTietHoaDon>(e =>
            {
                e.HasOne(ct => ct.HoaDonHocPhi)
                 .WithMany(hd => hd.ChiTietHoaDons)
                 .HasForeignKey(ct => ct.HoaDonHocPhiId)
                 .OnDelete(DeleteBehavior.Cascade);

           
                e.HasOne(ct => ct.DangKyHocPhan)
                 .WithMany()
                 .HasForeignKey(ct => ct.DangKyHocPhanId)
                 .OnDelete(DeleteBehavior.Restrict);
          
                e.Property(ct => ct.SoTien).HasColumnType("decimal(18, 2)");
            });

            // 3. DinhMucHocPhi
            builder.Entity<DinhMucHocPhi>(e =>
            {
                e.Property(d => d.GiaTienMotTinChi).HasColumnType("decimal(18, 2)");

                // Liên kết với Ngành
                e.HasOne(d => d.NganhHoc)
                 .WithMany()
                 .HasForeignKey(d => d.NganhId)
                 .OnDelete(DeleteBehavior.Restrict);

                // XÓA BỎ LIÊN KẾT VỚI KHOAHOC Ở ĐÂY NẾU CÓ
            });

            // 1. TaiKhoanSinhVien (1-1 với SinhVien)
            builder.Entity<TaiKhoanSinhVien>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.SoDu).HasColumnType("decimal(18, 2)");
                e.Property(t => t.RowVersion).IsRowVersion(); // Timestamp cho concurrency

                // Quan hệ 1-1: SinhVien là Principal (Cha), TaiKhoan là Dependent (Con)
                e.HasOne(t => t.SinhVien)
                 .WithOne() // Bên SinhVien có thể không cần property điều hướng ngược lại
                 .HasForeignKey<TaiKhoanSinhVien>(t => t.SinhVienId)
                 .OnDelete(DeleteBehavior.Cascade); // Xóa SV -> Xóa Tài khoản

                e.HasOne(t => t.TrangThai)
                 .WithMany()
                 .HasForeignKey(t => t.TrangThaiId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // 2. GiaoDichThanhToan
            builder.Entity<GiaoDichThanhToan>(e =>
            {
                e.Property(g => g.SoTien).HasColumnType("decimal(18, 2)");

                // Liên kết với TaiKhoanSinhVien (Bắt buộc)
                e.HasOne(g => g.TaiKhoanSinhVien)
                 .WithMany(tk => tk.LichSuGiaoDichs)
                 .HasForeignKey(g => g.TaiKhoanSinhVienId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Liên kết với HoaDonHocPhi (Có thể null - ví dụ nạp tiền)
                e.HasOne(g => g.HoaDonHocPhi)
                 .WithMany() // HoaDon không cần list GiaoDich (hoặc có thể thêm nếu muốn)
                 .HasForeignKey(g => g.HoaDonHocPhiId)
                 .OnDelete(DeleteBehavior.Restrict); // Không xóa Hóa đơn nếu đã có giao dịch
            });

        }
    }
}
