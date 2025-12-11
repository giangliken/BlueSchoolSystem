using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using BlueSchoolSystem.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace BlueSchoolSystem.Services
{
    // Không cần dùng Interface, trực tiếp tạo class Service
    public class LopHocPhanService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LopHocPhanService> _logger;
        private readonly Random _random = new Random();
        private readonly HocPhiService _hocPhiService;

        // Map Tiết học (Copy từ Controller cũ)
        private readonly Dictionary<int, string> TietStartMap = new Dictionary<int, string> {
            {1,"06:45"},{2,"07:30"},{3,"08:15"}, {4,"09:20"},{5,"10:05"},{6,"10:50"},
            {7,"12:30"},{8,"13:10"},{9,"14:00"}, {10,"15:05"},{11,"15:50"},{12,"16:35"},
            {13,"18:00"},{14,"18:45"},{15,"19:30"}
        };

        private readonly Dictionary<int, string> TietEndMap = new Dictionary<int, string> {
            {1,"07:30"},{2,"08:15"},{3,"09:00"}, {4,"10:05"},{5,"10:50"},{6,"11:35"},
            {7,"13:15"},{8,"13:55"},{9,"14:45"}, {10,"15:50"},{11,"16:35"},{12,"17:20"},
            {13,"18:45"},{14,"19:30"},{15,"20:15"}
        };

        public LopHocPhanService(ApplicationDbContext context, ILogger<LopHocPhanService> logger, HocPhiService hocPhiService)
        {
            _context = context;
            _logger = logger;
            _hocPhiService = hocPhiService;
        }

        // ====== HELPER METHODS ======

        public Dictionary<int, string> GetTietStartMap() => TietStartMap;
        public Dictionary<int, string> GetTietEndMap() => TietEndMap;

        public (TimeSpan start, TimeSpan end) ConvertTietToTimeSpan(int startTiet, int endTiet)
        {
            var startTime = TimeSpan.Parse(TietStartMap[startTiet]);
            var endTime = TimeSpan.Parse(TietEndMap[endTiet]);
            return (startTime, endTime);
        }

        public int ConvertDayOfWeekToCustomDay(DayOfWeek dayOfWeek) => dayOfWeek switch
        {
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 3,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 5,
            DayOfWeek.Friday => 6,
            DayOfWeek.Saturday => 7,
            DayOfWeek.Sunday => 8,
            _ => 0
        };

        // ====== BUSINESS LOGIC (DB QUERIES) ======

        public async Task<string> GenerateAutoMaLHPAsync(int hocKyId, int monHocId)
        {
            var hocKy = await _context.HocKys.FindAsync(hocKyId);
            var monHoc = await _context.MonHocs.FindAsync(monHocId);

            if (hocKy == null || monHoc == null) return null;

            string hocKyShort = hocKy.TenHocKy.Contains(" ")
                ? hocKy.TenHocKy.Substring(hocKy.TenHocKy.LastIndexOf(" ") + 1)
                : hocKy.TenHocKy;

            string maMon = monHoc.MaMonHoc.ToUpper();
            int year = hocKy.NgayBatDau.Year;
            int count = await _context.LopHocPhans
                .Where(l => l.HocKyId == hocKyId && l.MonHocId == monHocId)
                .CountAsync();

            return $"{hocKyShort.ToUpper()}-{maMon}-{year}-{(count + 1):D3}";
        }

        public async Task<LopHocPhan> GetLopHocPhanDetailsAsync(int id)
        {
            return await _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.GiangVien)
                .Include(l => l.TrangThai)
                .Include(l => l.HocKy)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<List<object>> GetLichHocByLHPIdAsync(int lhpId)
        {
            return await _context.LichHocs
                .Include(l => l.PhongHoc)
                .Where(l => l.LopHocPhanId == lhpId)
                .Select(l => new
                {
                    l.Id,
                    l.Ngay,
                    l.GioBatDau,
                    l.GioKetThuc,
                    Phong = l.PhongHoc.MaPhongHoc
                })
                .Cast<object>() // Chuyển sang List<object> để giữ ẩn danh
                .ToListAsync();
        }

        public async Task<List<GiangVien>> GetGiangVienByMonHocAsync(int? monHocId, string search = "")
        {
            var query = _context.GiangViens.AsQueryable();

            if (monHocId.HasValue && monHocId.Value > 0)
                query = query.Where(gv => gv.GiangVienMonHocs.Any(gvmh => gvmh.MonHocId == monHocId.Value));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(gv => (gv.HoVaTenDem + " " + gv.Ten).Contains(search));

            return await query.ToListAsync();
        }

        public async Task<List<PhongHoc>> GetPhongHocListAsync(int? monHocId)
        {
            var monHoc = monHocId.HasValue ? await _context.MonHocs.FindAsync(monHocId.Value) : null;
            var query = _context.PhongHocs.AsQueryable();

            if (monHoc != null && monHoc.MoTa != null && monHoc.MoTa.ToUpper().Contains("TH"))
            {
                // Môn Thực Hành -> Lấy Phòng Thực Hành
                query = query.Where(ph => ph.MaPhongHoc.ToUpper().Contains("TH") || ph.TenPhongHoc.ToUpper().Contains("THỰC HÀNH"));
            }
            else
            {
                // Môn Lý Thuyết -> Lấy Phòng Lý Thuyết
                query = query.Where(ph => !ph.MaPhongHoc.ToUpper().Contains("TH") && !ph.TenPhongHoc.ToUpper().Contains("THỰC HÀNH"));
            }

            return await query.ToListAsync();
        }

        public async Task<List<LichHocDTO>> CheckLichTrungAsync(List<LichHocDTO> newSchedules, int giangVienId, int lhpIdToExclude = 0)
        {
            var lichTrung = new List<LichHocDTO>();

            foreach (var newLich in newSchedules)
            {
                // Lọc bỏ LHP hiện tại nếu đang chỉnh sửa
                var baseQuery = _context.LichHocs
                    .Include(l => l.LopHocPhan)
                    .Where(l => l.LopHocPhanId != lhpIdToExclude &&
                                (newLich.GioBatDau < l.GioKetThuc) &&
                                (newLich.GioKetThuc > l.GioBatDau)); // Kiểm tra thời gian chồng lấn

                // Kiểm tra trùng phòng học
                var trungPhong = await baseQuery
                    .Where(l => l.PhongHocId == newLich.PhongHocId && l.Ngay == newLich.Ngay)
                    .AnyAsync();

                // Kiểm tra trùng lịch giảng viên
                var trungGiangVien = await baseQuery
                    .Where(l => l.LopHocPhan.GiangVienId == giangVienId && l.Ngay == newLich.Ngay)
                    .AnyAsync();

                if (trungPhong || trungGiangVien)
                {
                    lichTrung.Add(newLich);
                }
            }
            return lichTrung;
        }

        public async Task<(SelectList hocKyList, SelectList monHocList, SelectList phongHocList, SelectList giangVienList, List<TrangThai> trangThaiList)>
            PopulateCourseClassViewBagsAsync(LopHocPhanCreateDTO dto = null)
        {
            var validStatusNames = new List<string> { "Mới tạo", "Đang diễn ra" };
            var validStatusIds = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "HocKy" && validStatusNames.Contains(t.TenTrangThai))
                .Select(t => t.Id)
                .ToListAsync();

            var hocKys = await _context.HocKys
                .Where(hk => validStatusIds.Contains(hk.TrangThaiId))
                .ToListAsync();

            var monHocs = await _context.MonHocs.ToListAsync();

            // Lấy danh sách phòng học đầy đủ (chưa lọc theo môn học)
            var phongHocs = await _context.PhongHocs.ToListAsync();

            List<dynamic> giangViens = new List<dynamic>();

            if (dto?.MonHocId > 0)
            {
                // Lấy GV theo môn học
                giangViens = await _context.GiangViens
                    .Where(gv => gv.GiangVienMonHocs.Any(gvmh => gvmh.MonHocId == dto.MonHocId))
                    .Select(gv => new { gv.Id, HoTen = gv.HoVaTenDem + " " + gv.Ten })
                    .Cast<dynamic>()
                    .ToListAsync();
            }

            var trangThaiLHPList = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "LopHocPhan")
                .ToListAsync();

            var hocKyList = new SelectList(hocKys, "Id", "TenHocKy", dto?.HocKyId);
            var monHocList = new SelectList(monHocs, "Id", "TenMonHoc", dto?.MonHocId);
            var phongHocList = new SelectList(phongHocs, "Id", "MaPhongHoc", dto?.PhongHocId); // Lấy đầy đủ
            var giangVienList = new SelectList(giangViens, "Id", "HoTen", dto?.GiangVienId);

            return (hocKyList, monHocList, phongHocList, giangVienList, trangThaiLHPList);
        }

        public async Task<bool> DeleteCourseClassAsync(int lopHocPhanId)
        {
            var lhp = await _context.LopHocPhans
                .Include(l => l.LichHocs) 
                .FirstOrDefaultAsync(l => l.Id == lopHocPhanId);

            if (lhp == null) return false;

            // Xóa các lịch học trước
            _context.LichHocs.RemoveRange(lhp.LichHocs);

            // Xóa LHP
            _context.LopHocPhans.Remove(lhp);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- HELPER MỚI: Random Giảng viên ---
        private int? GetRandomGiangVienId(List<GiangVien> gvList)
        {
            if (!gvList.Any()) return null;
            int index = _random.Next(gvList.Count);
            return gvList[index].Id;
        }

        // --- HELPER MỚI: Random Lịch học (Tiết 2-6 hoặc 7-11, Ngày 2-7) ---
        public (int tietBatDau, int tietKetThuc, int dayOfWeek) GetRandomScheduleSettings()
        {
            bool isMorning = _random.Next(2) == 0;

            int tietBatDau = isMorning ? 2 : 7;
            int tietKetThuc = isMorning ? 6 : 11;

            int dayOfWeek = _random.Next(2, 8); // Thứ 2 -> Thứ 7

            return (tietBatDau, tietKetThuc, dayOfWeek);
        }

        // --- HELPER MỚI: Tạo danh sách các buổi học ---
        private List<LichHocDTO> GenerateWeeklySchedule(
     DateTime ngayBatDau,
     DateTime ngayKetThuc,
     int dayOfWeekCustom,
     int startTiet,
     int endTiet,
     int phongHocId,
     bool isThucHanh)
        {
            var schedules = new List<LichHocDTO>();
            var (gioBatDau, gioKetThuc) = ConvertTietToTimeSpan(startTiet, endTiet);

            // Chuyển 2..8 → Monday..Sunday
            DayOfWeek targetDay = dayOfWeekCustom == 8 ? DayOfWeek.Sunday : (DayOfWeek)(dayOfWeekCustom - 1);

            // Nếu là thực hành → bắt đầu sau LT đúng 2 tuần
            if (isThucHanh)
                ngayBatDau = ngayBatDau.AddDays(14);

            // Tìm ngày bắt đầu đầu tiên khớp thứ
            DateTime current = ngayBatDau;
            while (current.DayOfWeek != targetDay)
                current = current.AddDays(1);

            // Xác định số buổi theo LT/TH
            int totalSessions = isThucHanh ? 6 : 9;

            for (int i = 0; i < totalSessions; i++)
            {
                if (current > ngayKetThuc)
                    break;

                schedules.Add(new LichHocDTO
                {
                    LopHocPhanId = 0,
                    Ngay = current.Date,
                    GioBatDau = gioBatDau,
                    GioKetThuc = gioKetThuc,
                    PhongHocId = phongHocId
                });

                current = current.AddDays(7); // tuần kế tiếp
            }

            return schedules;
        }

        public async Task<(bool success, DateTime ngayBatDau, DateTime ngayKetThuc, string error)> GetHocKyDatesAsync(int hocKyId)
        {
            if (hocKyId <= 0)
                return (false, DateTime.MinValue, DateTime.MinValue, "Thiếu Học kỳ ID.");

            var hocKy = await _context.HocKys.FindAsync(hocKyId);

            if (hocKy == null)
                return (false, DateTime.MinValue, DateTime.MinValue, "Không tìm thấy Học kỳ.");

            return (true, hocKy.NgayBatDau, hocKy.NgayKetThuc, null);
        }
        public async Task<List<MonHoc>> GetMandatorySubjectsForAutoClassCreationAsync(
     int nganhId, int khoaNhapHoc, int thuTuHocKy)
        {
            // 1. Tìm Khóa học (ID) từ Năm nhập học
            var khoaHoc = await _context.KhoaHocs
                .FirstOrDefaultAsync(kh => kh.NamHoc == khoaNhapHoc);

            if (khoaHoc == null) return new List<MonHoc>();

            // 2. Lọc CTĐT dựa trên Ngành, KhóaHocId và Học kỳ
            var mandatorySubjectCodes = await _context.ChiTietChuongTrinhDaoTaos
                .Include(ct => ct.ChuongTrinhDaoTao)
                .Where(ct => ct.ChuongTrinhDaoTao.NganhHocId == nganhId
                             && ct.ChuongTrinhDaoTao.KhoaHocId == khoaHoc.Id
                             && ct.HocKy == thuTuHocKy
                             && ct.BatBuoc) // Dùng ct.BatBuoc
                .Select(ct => ct.MaMonHoc)
                .Distinct()
                .ToListAsync();

            if (!mandatorySubjectCodes.Any())
                return new List<MonHoc>();

            // 3. Tra cứu bảng MonHoc để lấy đối tượng MonHoc
            var subjects = await _context.MonHocs
                .Where(m => mandatorySubjectCodes.Contains(m.MaMonHoc))
                .ToListAsync();

            return subjects;
        }
        // Trong LopHocPhanService.cs
        // Phương thức mới để lấy thông tin chi tiết CTĐT
        private async Task<ChiTietChuongTrinhDaoTao> GetChiTietCTDTAsync(string maMonHoc, int nganhId, int khoaNhapHoc, int thuTuHocKy)
        {
            var khoaHoc = await _context.KhoaHocs
               .FirstOrDefaultAsync(kh => kh.NamHoc == khoaNhapHoc);

            if (khoaHoc == null) return null;

            return await _context.ChiTietChuongTrinhDaoTaos
                .Include(ct => ct.ChuongTrinhDaoTao)
                .FirstOrDefaultAsync(ct => ct.MaMonHoc == maMonHoc &&
                                           ct.HocKy == thuTuHocKy &&
                                           ct.ChuongTrinhDaoTao.NganhHocId == nganhId &&
                                           ct.ChuongTrinhDaoTao.KhoaHocId == khoaHoc.Id &&
                                           ct.BatBuoc);
        }

        // Phương thức kiểm tra sinh viên đã hoàn thành môn học chưa
        private async Task<bool> HasStudentCompletedSubjectAsync(int sinhVienId, string maMonHoc)
        {
            // Sinh viên đã có điểm (tức là đã hoàn thành) cho môn học này chưa
            return await _context.BangDiems
                .Include(bd => bd.LopHocPhan)
                    .ThenInclude(lhp => lhp.MonHoc)
                .AnyAsync(bd => bd.SinhVienId == sinhVienId &&
                                bd.LopHocPhan.MonHoc.MaMonHoc == maMonHoc &&
                                (bd.DiemCuoiKy.HasValue || bd.DiemChuyenCan.HasValue));
        }

        //// PHƯƠNG THỨC MỚI: Kiểm tra môn tiên quyết dựa trên ĐĂNG KÝ (cho kỳ hiện tại)
        //private async Task<bool> CanStudentEnrollBasedOnPrerequisiteAsync(
        //    int sinhVienId, int hocKyId, string maMonHocTienQuyet)
        //{
        //    if (string.IsNullOrWhiteSpace(maMonHocTienQuyet))
        //    {
        //        return true; // Không có môn tiên quyết
        //    }

        //    // B1: KIỂM TRA ĐÃ HOÀN THÀNH TỪ CÁC KỲ TRƯỚC (Dùng Bảng Điểm)
        //    var completed = await HasStudentCompletedSubjectAsync(sinhVienId, maMonHocTienQuyet);
        //    if (completed)
        //    {
        //        return true;
        //    }

        //    // B2: KIỂM TRA ĐÃ ĐĂNG KÝ TRONG KỲ HIỆN TẠI (Dùng DangKyHocPhan)
        //    // Đây là bước xử lý trường hợp môn lý thuyết và thực hành đi kèm nhau
        //    var isRegisteredInCurrentSemester = await _context.DangKyHocPhans
        //        .Include(dk => dk.LopHocPhan)
        //            .ThenInclude(lhp => lhp.MonHoc)
        //        .AnyAsync(dk => dk.SinhVienId == sinhVienId &&
        //                        dk.LopHocPhan.HocKyId == hocKyId &&
        //                        dk.LopHocPhan.MonHoc.MaMonHoc == maMonHocTienQuyet);

        //    if (isRegisteredInCurrentSemester)
        //    {
        //        return true;
        //    }

        //    // Nếu không hoàn thành ở kỳ trước, cũng không đăng ký trong kỳ này
        //    return false;
        //}

        private async Task<bool> CheckDieuKienTienQuyetAsync(int sinhVienId, int hocKyId, string? maMonTienQuyet)
        {
            // Nếu không có môn tiên quyết (null hoặc rỗng) -> Cho phép
            if (string.IsNullOrEmpty(maMonTienQuyet)) return true;

            // 1. Kiểm tra ĐÃ HỌC (Trong Bảng Điểm)
            // Điều kiện: Có bản ghi trong bảng điểm cho môn đó (bất kể kỳ nào trước đó)
            // Bạn có thể thêm điều kiện Điểm >= 4 nếu cần kiểm tra đậu/rớt
            bool daHoc = await _context.BangDiems
                .Include(bd => bd.LopHocPhan).ThenInclude(l => l.MonHoc)
                .AnyAsync(bd => bd.SinhVienId == sinhVienId
                             && bd.LopHocPhan.MonHoc.MaMonHoc == maMonTienQuyet);

            if (daHoc) return true;

            // 2. Kiểm tra ĐANG ĐĂNG KÝ (Song hành trong cùng kỳ hiện tại)
            // Ví dụ: Đăng ký Lý thuyết rồi thì được đăng ký Thực hành
            bool dangKyCungKy = await _context.DangKyHocPhans
                .Include(dk => dk.LopHocPhan).ThenInclude(l => l.MonHoc)
                .AnyAsync(dk => dk.SinhVienId == sinhVienId
                             && dk.LopHocPhan.HocKyId == hocKyId // Cùng học kỳ hiện tại
                             && dk.LopHocPhan.MonHoc.MaMonHoc == maMonTienQuyet);

            // Lưu ý: Nếu bạn vừa Add vào context mà chưa SaveChanges trong cùng 1 vòng lặp, 
            // truy vấn DB này có thể chưa thấy. 
            // Tuy nhiên, nếu bạn sắp xếp thứ tự môn học hợp lý (Lý thuyết trước TH) 
            // và có SaveChanges ở các bước trước đó thì ổn.

            // Kiểm tra thêm trong Local (bộ nhớ đệm) để bắt các môn vừa được Add trong cùng Transaction
            bool dangKyTrongBoNho = _context.DangKyHocPhans.Local
                .Any(dk => dk.SinhVienId == sinhVienId
                        && dk.LopHocPhan != null
                        && dk.LopHocPhan.HocKyId == hocKyId
                        && dk.LopHocPhan.MonHoc != null
                        && dk.LopHocPhan.MonHoc.MaMonHoc == maMonTienQuyet);

            return dangKyCungKy || dangKyTrongBoNho;
        }

        private async Task<List<int>> GetAllPhongHocIdsAsync()
        {
            return await _context.PhongHocs.Select(p => p.Id).ToListAsync();
        }

        // Trong class LopHocPhanService

        public async Task<(int classesCreated, int successCount, int schedulesCreated)> RunAutoEnrollmentJobAsync(AutoEnrollmentApiPayload dto)
        {
            // 1. XÁC ĐỊNH KHÓA HỌC VÀ SINH VIÊN MỤC TIÊU
            var khoaHoc = await _context.KhoaHocs.FirstOrDefaultAsync(kh => kh.NamHoc == dto.KhoaNhapHoc);
            if (khoaHoc == null) throw new InvalidOperationException($"Không tìm thấy Khóa học ({dto.KhoaNhapHoc}).");

            var targetStudents = await _context.SinhViens
                .Include(sv => sv.Lop)
                .Where(sv => sv.Lop != null && sv.Lop.NganhId == dto.NganhId)
                .Where(sv => sv.NgayNhapHoc.Year == dto.KhoaNhapHoc)
                .Where(sv => sv.TrangThai.TenTrangThai == "Đang học")
                .OrderBy(sv => sv.LopId)
                .ToListAsync();

            if (!targetStudents.Any()) throw new InvalidOperationException($"Không tìm thấy sinh viên mục tiêu để đăng ký.");

            // 2. THIẾT LẬP CƠ BẢN
            const int MAX_LHP_SIZE = 60;
            const int MAX_STUDENTS_PER_CLASS_IN_LHP = 50;
            var trangThaiChoMoId = _context.TrangThais.FirstOrDefault(t => t.LoaiTrangThai == "LopHocPhan" && t.TenTrangThai == "Đang mở")?.Id ?? 1;
            var hocKy = await _context.HocKys.FindAsync(dto.HocKyId);

            // Lấy danh sách TẤT CẢ Phòng học một lần để dùng cho Random
            var allPhongIds = await GetAllPhongHocIdsAsync();
            if (!allPhongIds.Any()) throw new Exception("Chưa có dữ liệu Phòng học.");

            // Các biến đếm kết quả
            int successCount = 0;
            int classesCreated = 0;
            int schedulesCreatedTotal = 0;
            var createdLhpsBySubject = new Dictionary<string, List<LopHocPhan>>();

            // Lấy tham số lịch học từ DTO
            int inputTietBatDau = dto.TietBatDau;
            int inputTietKetThuc = dto.TietKetThuc;
            int inputPhongHocId = dto.DefaultPhongHocId;
            var availableDays = dto.CacNgayTrongTuan.ToList();

            // Chuẩn bị danh sách để Batch Insert
            var newRegistrations = new List<DangKyHocPhan>();
            var newDetails = new List<ChiTietLopHocPhan>();

            // 3. VÒNG LẶP XỬ LÝ TỪNG MÔN HỌC
            foreach (var maMonHoc in dto.MandatorySubjectCodes)
            {
                var monHoc = await _context.MonHocs.FirstOrDefaultAsync(m => m.MaMonHoc == maMonHoc);
                if (monHoc == null) continue;

                var chiTietCtdt = await GetChiTietCTDTAsync(maMonHoc, dto.NganhId, dto.KhoaNhapHoc, dto.ThuTuHocKy);
                bool isThucHanh = monHoc.MoTa?.ToUpper().Contains("TH") ?? false;
                int maxAllowedSessions = isThucHanh ? 6 : 9;
                string? maMonTienQuyet = chiTietCtdt?.MaMonHocTienQuyet;

                // Tìm GV
                var gvCandidates = await GetGiangVienByMonHocAsync(monHoc.Id);
                // (Chúng ta sẽ random GV lại ở bên dưới cho mỗi lớp, ở đây chỉ check tồn tại)
                if (!gvCandidates.Any()) continue;

                if (!createdLhpsBySubject.ContainsKey(maMonHoc)) createdLhpsBySubject[maMonHoc] = new List<LopHocPhan>();

                // Lọc sinh viên cần học
                var studentsToEnroll = new List<SinhVien>();
                foreach (var sv in targetStudents)
                {
                    if (!await HasStudentCompletedSubjectAsync(sv.Id, maMonHoc)) studentsToEnroll.Add(sv);
                }

                if (!studentsToEnroll.Any()) continue;

                // Xáo trộn danh sách ngày để đảm bảo random assignment mỗi lần tạo lớp mới
                // Lưu ý: Việc xáo trộn này nên thực hiện mỗi khi cần tìm slot mới, nhưng để đơn giản ta làm ở đây
                // Logic bên dưới sẽ xáo trộn lại mỗi khi cần tìm slot.

                // Lặp qua tất cả sinh viên cần đăng ký, cố gắng phân bổ
                foreach (var sv in studentsToEnroll)
                {
                    bool duDieuKien = await CheckDieuKienTienQuyetAsync(sv.Id, dto.HocKyId, maMonTienQuyet);
                    // Kiểm tra Tiên Quyết
                    if (!duDieuKien)
                    {
                       continue;
                    }

                    LopHocPhan selectedLhp = null;

                    // A. Tìm LHP đã có còn chỗ (Ưu tiên cùng lớp hành chính)
                    foreach (var existingLhp in createdLhpsBySubject[maMonHoc])
                    {
                        int currentSiSo = _context.DangKyHocPhans.Count(dk => dk.LopHocPhanId == existingLhp.Id);
                        if (currentSiSo >= MAX_LHP_SIZE) continue;

                        int studentsFromSameClass = _context.DangKyHocPhans
                            .Include(dk => dk.SinhVien)
                            .Count(dk => dk.LopHocPhanId == existingLhp.Id && dk.SinhVien.LopId == sv.LopId);

                        if (studentsFromSameClass < MAX_STUDENTS_PER_CLASS_IN_LHP)
                        {
                            selectedLhp = existingLhp;
                            break;
                        }
                    }

                    // B. Tạo LHP Mới nếu không tìm thấy lớp trống
                    if (selectedLhp == null)
                    {
                        int finalDayOfWeek = 0;
                        int finalTietBd = 0;
                        int finalTietKt = 0;
                        int finalPhongId = 0;
                        int? finalGiangVienId = null;
                        bool foundValidSlot = false;

                        // Xáo trộn danh sách ngày để thử ngẫu nhiên
                        var daysToSearch = dto.CacNgayTrongTuan.OrderBy(x => Guid.NewGuid()).ToList();

                        // --- VÒNG LẶP TÌM KIẾM SLOT ---
                        // Thử từng ngày một
                        foreach (var day in daysToSearch)
                        {
                            // TẠI MỖI NGÀY, THỬ RANDOM CẤU HÌNH (TIẾT/PHÒNG/GV) MỚI
                            // Thử tối đa 10 lần random cho mỗi ngày để tìm slot trống
                            for (int attempt = 0; attempt < 10; attempt++)
                            {
                                // 1. Random Tiết
                                var sched = GetRandomScheduleSettings();
                                int tryTietBd = sched.tietBatDau;
                                int tryTietKt = sched.tietKetThuc;

                                // 2. Random Phòng
                                int tryPhongId = allPhongIds[_random.Next(allPhongIds.Count)];

                                // 3. Random GV
                                int? tryGvId = GetRandomGiangVienId(gvCandidates);
                                if (tryGvId == null) break;

                                // 4. Tạo lịch giả định
                                var schedulesToTest = GenerateWeeklySchedule(
                                    hocKy.NgayBatDau, hocKy.NgayKetThuc,
                                    day, tryTietBd, tryTietKt,
                                    tryPhongId, isThucHanh).Take(maxAllowedSessions).ToList();

                                // 5. Kiểm tra trùng
                                var conflicts = await CheckLichTrungAsync(schedulesToTest, tryGvId.Value, 0);

                                if (!conflicts.Any())
                                {
                                    // TÌM THẤY! Lưu lại thông số
                                    finalDayOfWeek = day;
                                    finalTietBd = tryTietBd;
                                    finalTietKt = tryTietKt;
                                    finalPhongId = tryPhongId;
                                    finalGiangVienId = tryGvId;
                                    foundValidSlot = true;
                                    break; // Thoát vòng lặp attempt
                                }
                            }
                            if (foundValidSlot) break; // Thoát vòng lặp ngày
                        }

                        if (!foundValidSlot)
                        {
                            _logger.LogWarning($"[AUTO] Không tìm được lịch cho môn {maMonHoc} sau nhiều lần thử.");
                            continue; // Bỏ qua SV này
                        }

                        // --- TẠO LỚP VỚI THÔNG SỐ ĐÃ TÌM ĐƯỢC ---
                        var maLopHocPhan = await GenerateAutoMaLHPAsync(dto.HocKyId, monHoc.Id);
                        selectedLhp = new LopHocPhan
                        {
                            HocKyId = dto.HocKyId,
                            MonHocId = monHoc.Id,
                            GiangVienId = finalGiangVienId, // GV Random được chọn
                            TrangThaiId = trangThaiChoMoId,
                            MaLopHocPhan = maLopHocPhan,
                            TenLopHocPhan = $"{monHoc.TenMonHoc}",
                            SiSo = MAX_LHP_SIZE,
                            NgayBatDau = hocKy.NgayBatDau,
                            NgayKetThuc = hocKy.NgayKetThuc
                        };

                        _context.LopHocPhans.Add(selectedLhp);
                        await _context.SaveChangesAsync();
                        createdLhpsBySubject[maMonHoc].Add(selectedLhp);
                        classesCreated++;

                        // --- LƯU LỊCH HỌC ---
                        if (dto.ShouldAutoCreateSchedule)
                        {
                            var finalSchedules = GenerateWeeklySchedule(
                                    hocKy.NgayBatDau, hocKy.NgayKetThuc,
                                    finalDayOfWeek, finalTietBd, finalTietKt,
                                    finalPhongId, isThucHanh).Take(maxAllowedSessions).ToList();

                            foreach (var lich in finalSchedules)
                            {
                                _context.LichHocs.Add(new LichHoc
                                {
                                    LopHocPhanId = selectedLhp.Id,
                                    Ngay = lich.Ngay.Date,
                                    GioBatDau = lich.GioBatDau,
                                    GioKetThuc = lich.GioKetThuc,
                                    PhongHocId = lich.PhongHocId
                                });
                                schedulesCreatedTotal++;
                            }
                            await _context.SaveChangesAsync();
                        }
                    } // Kết thúc khối IF (selectedLhp == null)

                    // C. Đăng ký sinh viên vào LHP đã chọn/tạo
                    var alreadyRegistered = await _context.DangKyHocPhans
                        .AnyAsync(dk => dk.SinhVienId == sv.Id && dk.LopHocPhanId == selectedLhp.Id);

                    if (!alreadyRegistered)
                    {
                        var dkMoi = new DangKyHocPhan
                        {
                            SinhVienId = sv.Id,
                            LopHocPhanId = selectedLhp.Id,
                            NgayDangKy = DateTime.Now,
                            LoaiDangKy = "BatBuocAuto"
                        };
                        newRegistrations.Add(dkMoi); // Add vào List tạm

                        newDetails.Add(new ChiTietLopHocPhan
                        {
                            LopHocPhanId = selectedLhp.Id,
                            SinhVienId = sv.Id
                        });

                        successCount++;
                    }
                }
            }

            // 4. LƯU VÀO DB MỘT LẦN DUY NHẤT (Batch Insert)
            if (newRegistrations.Any())
            {
                _context.DangKyHocPhans.AddRange(newRegistrations);
                _context.ChiTietLopHocPhans.AddRange(newDetails);

                await _context.SaveChangesAsync(); // Lúc này EF sẽ tự điền ID vào newRegistrations
            }

            // 5. TÍNH TIỀN HÀNG LOẠT (Sau khi đã có ID)
            foreach (var dk in newRegistrations)
            {
                try
                {
                    // Gọi hàm tính tiền với ID thật vừa được sinh ra
                    // Lưu ý: Nếu user "System" không tồn tại trong bảng User, hãy đảm bảo hàm LogAsync xử lý được case này (hoặc truyền null)
                    await _hocPhiService.GhiNoHocPhiAsync(dk.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi tính phí SV {dk.SinhVienId}: {ex.Message}");
                }
            }

            return (classesCreated, successCount, schedulesCreatedTotal);
        }


        public async Task<(bool success, string message)> CreateDotDangKyForSelectedLhpsAsync(
    QuanLyDangKyLHPRequestDTO dto,
    string userId,
    string userName,
    string device,
    string ipAddress)
        {
            // 1. Kiểm tra ngày tháng
            if (dto.NgayBatDau >= dto.NgayKetThuc)
            {
                return (false, "Ngày bắt đầu phải trước ngày kết thúc.");
            }

            var trangThaiChoMo = await _context.TrangThais
   .FirstOrDefaultAsync(t => t.LoaiTrangThai == "LopHocPhan" && t.TenTrangThai == "Chờ mở");

            if (trangThaiChoMo == null)
                return (false, "Không tìm thấy trạng thái 'Chờ mở'.");

            // 2. Lấy thông tin LHP đã chọn và kiểm tra tính hợp lệ
            // Lấy chi tiết LHP (cần MonHoc để check xem có bị trùng DotDangKy LHP khác không)
            var selectedLhps = await _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.TrangThai)
                .Where(l => dto.LopHocPhanIds.Contains(l.Id)
                    && l.HocKyId == dto.HocKyId
                    && l.TrangThaiId == trangThaiChoMo.Id)       
                .ToListAsync();

            // Lọc ra các LHP không tồn tại hoặc không thuộc Học kỳ đang chọn
            if (selectedLhps.Count != dto.LopHocPhanIds.Count)
            {
                var foundIds = selectedLhps.Select(l => l.Id).ToList();
                var missingIds = dto.LopHocPhanIds.Except(foundIds).ToList();
                return (false, $"Các Lớp Học Phần ID sau không hợp lệ/không thuộc Học kỳ {dto.HocKyId}: {string.Join(", ", missingIds)}.");
            }

           

            // Kiểm tra LHP còn đủ điều kiện mở đăng ký (Chờ mở và còn chỗ)
            var trangThaiDangMo = await _context.TrangThais
        .FirstOrDefaultAsync(t => t.LoaiTrangThai == "LopHocPhan" && t.TenTrangThai == "Đang mở");

            if (trangThaiDangMo == null) return (false, "Lỗi hệ thống: Không tìm thấy trạng thái 'Đang mở'.");

            // 3. Chuẩn bị bản ghi Đợt Đăng ký
            var newDotDangKys = new List<DotDangKy>();

            foreach (var lhp in selectedLhps)
            {
                // 🚨 Kiểm tra trùng: Ngăn tạo DotDangKy cho cùng một LHP (hoặc cùng một MonHoc) trong cùng một khoảng thời gian
                bool isConflict = await _context.DotDangKys
                    .Where(d => d.HocKyId == dto.HocKyId)
                    // Lọc theo LHP ID hoặc Lọc theo MonHoc Code (chọn LHP ID cho tính cụ thể)
                    .Where(d => d.LoaiDoiTuong == "LHP" && d.GiaTriDoiTuong == lhp.Id.ToString())
                    // Kiểm tra thời gian chồng lấn
                    .Where(d => d.NgayBatDau < dto.NgayKetThuc && d.NgayKetThuc > dto.NgayBatDau)
                    .AnyAsync();

                if (isConflict)
                {
                    // Trả về lỗi nếu có LHP bị trùng lịch đăng ký
                    return (false, $"Lỗi trùng lặp: Lớp HP {lhp.MaLopHocPhan} đã có đợt đăng ký chồng lấn trong khoảng thời gian này.");
                }

                // Tạo bản ghi DotDangKy cho LHP cụ thể này
                var dot = new DotDangKy
                {
                    HocKyId = dto.HocKyId,
                    TenDot = dto.TenDotDangKy,
                    NgayBatDau = dto.NgayBatDau,
                    NgayKetThuc = dto.NgayKetThuc,
                    LoaiThaoTac = "DANGKY", // Luôn là Đăng ký khi dùng chức năng này
                    LoaiDoiTuong = "LHP", // Đối tượng áp dụng là Lớp Học Phần
                    GiaTriDoiTuong = lhp.Id.ToString(), // Lưu ID của LHP
                    IsActive = true
                };
                newDotDangKys.Add(dot);

                lhp.TrangThaiId = trangThaiDangMo.Id;

                // Đánh dấu object đã bị sửa đổi (để EF Core biết cần Update)
                _context.Entry(lhp).State = EntityState.Modified;
            }

            if (!newDotDangKys.Any())
            {
                return (false, "Không có Lớp Học Phần nào hợp lệ để tạo đợt đăng ký.");
            }

            // 4. Lưu vào DB
            _context.DotDangKys.AddRange(newDotDangKys);
            await _context.SaveChangesAsync();

           

            return (true, $"Đã mở đăng ký đợt '{dto.TenDotDangKy}' cho {selectedLhps.Count} Lớp Học Phần thành công.");
        }

        //
        public async Task<List<SinhVien>> GetStudentsInClassAsync(int lhpId)
        {
            // Truy vấn từ bảng ChiTietLopHocPhan, kết nối sang SinhVien và các bảng liên quan
            var students = await _context.ChiTietLopHocPhans
                .Where(ct => ct.LopHocPhanId == lhpId)
                .Include(ct => ct.SinhVien)
                    .ThenInclude(sv => sv.Lop) // Lấy thông tin Lớp hành chính
                .Include(ct => ct.SinhVien)
                    .ThenInclude(sv => sv.User) // Lấy Email, SĐT từ User
                .Select(ct => ct.SinhVien) // Chỉ lấy đối tượng SinhVien ra
                .OrderBy(sv => sv.Ten) // Sắp xếp theo tên
                .ThenBy(sv => sv.HoVaTenDem)
                .ToListAsync();

            return students;
        }

        #region==== HELPERS CHO ĐĂNG KÝ HỌC PHẦN SINH VIÊN ====
        // 1. Lấy danh sách lớp học phần đang mở trong kỳ
        public async Task<List<LopHocPhan>> GetLopMoDangKyAsync(int hocKyId)
        {
            return await _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.GiangVien)
                .Include(l => l.LichHocs).ThenInclude(lh => lh.PhongHoc) // Include Phòng để hiển thị
                .Include(l => l.TrangThai)
                .Where(l => l.HocKyId == hocKyId)
                .Where(l => l.TrangThai.TenTrangThai == "Đang mở") // Uncomment nếu muốn lọc chỉ lớp đang mở
                .ToListAsync();
        }

        // 2. Lấy danh sách ID các lớp mà sinh viên đã đăng ký trong kỳ
        public async Task<List<int>> GetDaDangKyIdsAsync(int sinhVienId, int hocKyId)
        {
            return await _context.DangKyHocPhans
                .Where(dk => dk.SinhVienId == sinhVienId && dk.LopHocPhan.HocKyId == hocKyId)
                .Select(dk => dk.LopHocPhanId)
                .ToListAsync();
        }

        // 3. Đếm số lượng đã đăng ký hiện tại của 1 lớp
        public async Task<int> CountSiSoHienTaiAsync(int lopHocPhanId)
        {
            return await _context.DangKyHocPhans.CountAsync(dk => dk.LopHocPhanId == lopHocPhanId);
        }

        // 4. Hàm Format lịch học (Logic cũ chuyển từ Controller sang đây để tái sử dụng)
        public string FormatLichHoc(LopHocPhan lhp)
        {
            if (lhp.LichHocs == null || !lhp.LichHocs.Any()) return "Chưa có lịch";

            var uniqueSchedules = lhp.LichHocs
                .GroupBy(lh => new {
                    DayOfWeek = lh.Ngay.DayOfWeek,
                    Start = lh.GioBatDau,
                    End = lh.GioKetThuc
                })
                .Select(g => g.First())
                .OrderBy(lh => lh.Ngay.DayOfWeek)
                .ToList();

            var listStr = new List<string>();
            foreach (var lh in uniqueSchedules)
            {
                // Chuyển đổi DayOfWeek sang tiếng Việt
                string thu = lh.Ngay.DayOfWeek == DayOfWeek.Sunday ? "CN" : "T" + ((int)lh.Ngay.DayOfWeek + 1);

                // Tìm tiết bắt đầu/kết thúc từ Dictionary có sẵn trong Service
                // Logic so sánh TimeSpan (tương đối)
                int tietBD = TietStartMap.FirstOrDefault(x => TimeSpan.Parse(x.Value) == lh.GioBatDau).Key;
                int tietKT = TietEndMap.FirstOrDefault(x => TimeSpan.Parse(x.Value) == lh.GioKetThuc).Key;

                string timeStr = (tietBD > 0 && tietKT > 0)
                    ? $"Tiết {tietBD}-{tietKT}"
                    : $"{lh.GioBatDau:hh\\:mm}-{lh.GioKetThuc:hh\\:mm}";

                listStr.Add($"{thu} ({timeStr}) - {lh.PhongHoc?.MaPhongHoc ?? "P.Unknown"}");
            }

            return string.Join(", ", listStr);
        }
        #endregion
    }
}