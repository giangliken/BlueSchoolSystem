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
    public class EnrollmentStats
    {
        public int ClassesCreated { get; set; } = 0;
        public int SuccessCount { get; set; } = 0;
        public int SchedulesCreated { get; set; } = 0;
    }
    public class LopHocPhanService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LopHocPhanService> _logger;
        private readonly Random _random = new Random();
        private readonly HocPhiService _hocPhiService;
        private readonly LichThiService _lichThiService;

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

        public LopHocPhanService(ApplicationDbContext context, ILogger<LopHocPhanService> logger, HocPhiService hocPhiService, LichThiService lichThiService)
        {
            _context = context;
            _logger = logger;
            _hocPhiService = hocPhiService;
            _lichThiService = lichThiService;
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

        // =========================================================================================
        // HÀM CHÍNH: CHẠY TỰ ĐỘNG ĐĂNG KÝ
        // =========================================================================================
        public async Task<(int classesCreated, int successCount, int schedulesCreated, int examsCreated)> RunAutoEnrollmentJobAsync(AutoEnrollmentApiPayload dto)
        {
            const int MAX_LHP_SIZE = 60;
            const int THRESHOLD_SEPARATE_CLASS = (int)(MAX_LHP_SIZE * 2.0 / 3.0); // 40 students

            var trangThaiChoMoId = _context.TrangThais.FirstOrDefault(t => t.LoaiTrangThai == "LopHocPhan" && t.TenTrangThai == "Đang mở")?.Id ?? 1;
            var hocKy = await _context.HocKys.FindAsync(dto.HocKyId);
            var allPhongIds = await GetAllPhongHocIdsAsync();

            if (hocKy == null) throw new InvalidOperationException("Học kỳ không tồn tại.");
            if (!allPhongIds.Any()) throw new Exception("Chưa có dữ liệu Phòng học.");

            var stats = new EnrollmentStats();
            var allCreatedLhps = new List<LopHocPhan>();
            int examsCreatedTotal = 0;

            var newRegistrations = new List<DangKyHocPhan>();
            var newDetails = new List<ChiTietLopHocPhan>();

            // Map to track class busy schedules: Key = LopId, Value = List<LichHocDTO>
            var classBusySchedules = new Dictionary<int, List<LichHocDTO>>();

            var targetStudents = await _context.SinhViens
                .Include(sv => sv.Lop)
                .Where(sv => sv.Lop != null && sv.Lop.NganhId == dto.NganhId)
                .Where(sv => sv.NgayNhapHoc.Year == dto.KhoaNhapHoc)
                .Where(sv => sv.TrangThai.TenTrangThai == "Đang học")
                .OrderBy(sv => sv.LopId)
                .ToListAsync();

            if (!targetStudents.Any()) throw new InvalidOperationException("Không tìm thấy sinh viên mục tiêu.");

            foreach (var maMonHoc in dto.MandatorySubjectCodes)
            {
                var monHoc = await _context.MonHocs.FirstOrDefaultAsync(m => m.MaMonHoc == maMonHoc);
                if (monHoc == null) continue;

                var chiTietCtdt = await GetChiTietCTDTAsync(maMonHoc, dto.NganhId, dto.KhoaNhapHoc, dto.ThuTuHocKy);
                bool isThucHanh = monHoc.MoTa?.ToUpper().Contains("TH") ?? false;
                int maxAllowedSessions = isThucHanh ? 6 : 9;
                string? maMonTienQuyet = chiTietCtdt?.MaMonHocTienQuyet;

                var gvCandidates = await GetGiangVienByMonHocAsync(monHoc.Id);
                if (!gvCandidates.Any())
                {
                    _logger.LogWarning($"Bỏ qua môn {maMonHoc} vì không tìm thấy giảng viên phụ trách.");
                    continue;
                }

                var validStudents = new List<SinhVien>();
                foreach (var sv in targetStudents)
                {
                    bool isCompleted = await HasStudentCompletedSubjectAsync(sv.Id, maMonHoc);
                    bool isRegistered = await _context.DangKyHocPhans
                        .AnyAsync(dk => dk.SinhVienId == sv.Id && dk.LopHocPhan.HocKyId == dto.HocKyId && dk.LopHocPhan.MonHoc.MaMonHoc == maMonHoc)
                        || newRegistrations.Any(r => r.SinhVienId == sv.Id && allCreatedLhps.Any(l => l.Id == r.LopHocPhanId && l.MonHocId == monHoc.Id));

                    if (!isCompleted && !isRegistered)
                    {
                        if (await CheckDieuKienTienQuyetAsync(sv.Id, dto.HocKyId, maMonTienQuyet))
                        {
                            validStudents.Add(sv);
                        }
                    }
                }

                if (!validStudents.Any()) continue;

                var studentGroupsByClass = validStudents
                    .GroupBy(s => s.LopId)
                    .Select(g => new { LopId = g.Key, Students = g.ToList() })
                    .ToList();

                var pendingGroups = new List<List<SinhVien>>();

                foreach (var group in studentGroupsByClass)
                {
                    var studentsInClass = group.Students;
                    int count = studentsInClass.Count;
                    int? currentLopId = group.LopId;

                    if (currentLopId.HasValue && !classBusySchedules.ContainsKey(currentLopId.Value))
                    {
                        classBusySchedules[currentLopId.Value] = new List<LichHocDTO>();
                    }
                    var currentBusySchedule = currentLopId.HasValue ? classBusySchedules[currentLopId.Value] : new List<LichHocDTO>();

                    if (count > THRESHOLD_SEPARATE_CLASS)
                    {
                        int fullClasses = count / MAX_LHP_SIZE;
                        int remainder = count % MAX_LHP_SIZE;
                        int currentIndex = 0;

                        for (int i = 0; i < fullClasses; i++)
                        {
                            var batch = studentsInClass.GetRange(currentIndex, MAX_LHP_SIZE);
                            await CreateLhpAndEnrollAsync(batch, monHoc, hocKy, gvCandidates, allPhongIds, isThucHanh, maxAllowedSessions,
                                trangThaiChoMoId, dto, newRegistrations, newDetails, allCreatedLhps, stats, currentBusySchedule);
                            currentIndex += MAX_LHP_SIZE;
                        }

                        if (remainder > 0)
                        {
                            var remainingStudents = studentsInClass.GetRange(currentIndex, remainder);
                            if (remainder > THRESHOLD_SEPARATE_CLASS)
                            {
                                await CreateLhpAndEnrollAsync(remainingStudents, monHoc, hocKy, gvCandidates, allPhongIds, isThucHanh, maxAllowedSessions,
                                    trangThaiChoMoId, dto, newRegistrations, newDetails, allCreatedLhps, stats, currentBusySchedule);
                            }
                            else
                            {
                                pendingGroups.Add(remainingStudents);
                            }
                        }
                    }
                    else
                    {
                        pendingGroups.Add(studentsInClass);
                    }
                }

                var currentBatch = new List<SinhVien>();
                var batchBusySchedules = new List<LichHocDTO>();

                foreach (var group in pendingGroups)
                {
                    if (currentBatch.Count + group.Count <= MAX_LHP_SIZE)
                    {
                        currentBatch.AddRange(group);
                        int? lopId = group.FirstOrDefault()?.LopId;
                        if (lopId.HasValue && classBusySchedules.ContainsKey(lopId.Value))
                        {
                            batchBusySchedules.AddRange(classBusySchedules[lopId.Value]);
                        }
                    }
                    else
                    {
                        if (currentBatch.Any())
                        {
                            await CreateLhpAndEnrollAsync(currentBatch, monHoc, hocKy, gvCandidates, allPhongIds, isThucHanh, maxAllowedSessions,
                                trangThaiChoMoId, dto, newRegistrations, newDetails, allCreatedLhps, stats, batchBusySchedules);
                        }
                        currentBatch = new List<SinhVien>(group);
                        batchBusySchedules = new List<LichHocDTO>();
                        int? lopId = group.FirstOrDefault()?.LopId;
                        if (lopId.HasValue && classBusySchedules.ContainsKey(lopId.Value))
                        {
                            batchBusySchedules.AddRange(classBusySchedules[lopId.Value]);
                        }
                    }
                }

                if (currentBatch.Any())
                {
                    await CreateLhpAndEnrollAsync(currentBatch, monHoc, hocKy, gvCandidates, allPhongIds, isThucHanh, maxAllowedSessions,
                        trangThaiChoMoId, dto, newRegistrations, newDetails, allCreatedLhps, stats, batchBusySchedules);
                }
            }

            if (newRegistrations.Any())
            {
                _context.DangKyHocPhans.AddRange(newRegistrations);
                _context.ChiTietLopHocPhans.AddRange(newDetails);
                await _context.SaveChangesAsync();
            }

            foreach (var dk in newRegistrations)
            {
                try { await _hocPhiService.GhiNoHocPhiAsync(dk.Id); } catch { }
            }

            foreach (var lhp in allCreatedLhps.DistinctBy(l => l.Id))
            {
                try
                {
                    var classIds = await _context.ChiTietLopHocPhans
                        .Where(ct => ct.LopHocPhanId == lhp.Id && ct.SinhVien.LopId.HasValue)
                        .Select(ct => ct.SinhVien.LopId.Value)
                        .Distinct().ToListAsync();

                    if (!classIds.Any()) continue;
                    var examResult = await _lichThiService.AutoScheduleExamAsync(lhp.Id, classIds);
                    if (examResult.Success) examsCreatedTotal++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi tạo lịch thi cho LHP {lhp.Id}");
                }
            }

            return (stats.ClassesCreated, stats.SuccessCount, stats.SchedulesCreated, examsCreatedTotal);
        }


        private async Task CreateLhpAndEnrollAsync(
            List<SinhVien> students,
            MonHoc monHoc,
            HocKy hocKy,
            List<GiangVien> gvCandidates,
            List<int> allPhongIds,
            bool isThucHanh,
            int maxAllowedSessions,
            int trangThaiId,
            AutoEnrollmentApiPayload dto,
            List<DangKyHocPhan> regList,
            List<ChiTietLopHocPhan> detailList,
            List<LopHocPhan> createdLhps,
            EnrollmentStats stats,
            List<LichHocDTO> existingClassSchedules)
        {
            int finalDay = 0, finalTietBd = 0, finalTietKt = 0, finalPhongId = 0;
            int? finalGvId = null;
            bool foundSlot = false;
            List<LichHocDTO> finalSchedulesToSave = new List<LichHocDTO>();

            for (int i = 0; i < 100; i++)
            {
                var sched = GetRandomScheduleSettings();
                if (!dto.CacNgayTrongTuan.Contains(sched.dayOfWeek)) continue;

                int tryPhong = allPhongIds[_random.Next(allPhongIds.Count)];
                int? tryGv = GetRandomGiangVienId(gvCandidates);
                if (tryGv == null) break;

                var testSchedules = GenerateWeeklySchedule(hocKy.NgayBatDau, hocKy.NgayKetThuc, sched.dayOfWeek,
                    sched.tietBatDau, sched.tietKetThuc, tryPhong, isThucHanh).Take(maxAllowedSessions).ToList();

                var conflicts = await CheckLichTrungAsync(testSchedules, tryGv.Value);
                if (conflicts.Any()) continue;

                bool studentConflict = false;
                foreach (var ts in testSchedules)
                {
                    bool clash = existingClassSchedules.Any(ex =>
                        ex.Ngay.Date == ts.Ngay.Date &&
                        (ts.GioBatDau < ex.GioKetThuc && ts.GioKetThuc > ex.GioBatDau)
                    );

                    if (clash)
                    {
                        studentConflict = true;
                        break;
                    }
                }

                if (!studentConflict)
                {
                    finalDay = sched.dayOfWeek;
                    finalTietBd = sched.tietBatDau;
                    finalTietKt = sched.tietKetThuc;
                    finalPhongId = tryPhong;
                    finalGvId = tryGv;
                    finalSchedulesToSave = testSchedules;
                    foundSlot = true;
                    break;
                }
            }

            if (!foundSlot)
            {
                _logger.LogWarning($"[AUTO-FAIL] Không tìm được lịch cho môn {monHoc.MaMonHoc} (Lớp {students.FirstOrDefault()?.Lop?.MaLop}) sau 100 lần thử.");
                return;
            }

            var maLhp = await GenerateAutoMaLHPAsync(dto.HocKyId, monHoc.Id);
            var newLhp = new LopHocPhan
            {
                HocKyId = dto.HocKyId,
                MonHocId = monHoc.Id,
                GiangVienId = finalGvId,
                TrangThaiId = trangThaiId,
                MaLopHocPhan = maLhp,
                TenLopHocPhan = monHoc.TenMonHoc,
                SiSo = 60,
                NgayBatDau = hocKy.NgayBatDau,
                NgayKetThuc = hocKy.NgayKetThuc
            };

            _context.LopHocPhans.Add(newLhp);
            await _context.SaveChangesAsync();
            createdLhps.Add(newLhp);
            stats.ClassesCreated++;

            if (dto.ShouldAutoCreateSchedule)
            {
                foreach (var lich in finalSchedulesToSave)
                {
                    lich.LopHocPhanId = newLhp.Id;
                    _context.LichHocs.Add(new LichHoc
                    {
                        LopHocPhanId = newLhp.Id,
                        Ngay = lich.Ngay,
                        GioBatDau = lich.GioBatDau,
                        GioKetThuc = lich.GioKetThuc,
                        PhongHocId = lich.PhongHocId
                    });
                    stats.SchedulesCreated++;
                    existingClassSchedules.Add(lich);
                }
                await _context.SaveChangesAsync();
            }

            foreach (var sv in students)
            {
                regList.Add(new DangKyHocPhan
                {
                    SinhVienId = sv.Id,
                    LopHocPhanId = newLhp.Id,
                    NgayDangKy = DateTime.Now,
                    LoaiDangKy = "BatBuocAuto"
                });

                detailList.Add(new ChiTietLopHocPhan
                {
                    LopHocPhanId = newLhp.Id,
                    SinhVienId = sv.Id
                });
                stats.SuccessCount++;
            }
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

        public async Task<(bool isConflict, string conflictDetails)> CheckTrungLichSinhVienAsync(int sinhVienId, int lopHocPhanMoiId)
        {
            // 1. Lấy thông tin lớp mới và lịch học của nó
            var lhpMoi = await _context.LopHocPhans
                .Include(l => l.LichHocs)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lopHocPhanMoiId);

            if (lhpMoi == null || lhpMoi.LichHocs == null || !lhpMoi.LichHocs.Any())
                return (false, string.Empty); // Lớp mới không có lịch -> Không trùng

            // 2. Lấy danh sách các lớp ĐÃ ĐĂNG KÝ của SV trong cùng học kỳ (Trừ lớp đang xét nếu có)
            var cacLopDaDangKyIds = await _context.DangKyHocPhans
                .Where(dk => dk.SinhVienId == sinhVienId
                          && dk.LopHocPhan.HocKyId == lhpMoi.HocKyId
                          && dk.LopHocPhanId != lopHocPhanMoiId)
                .Select(dk => dk.LopHocPhanId)
                .ToListAsync();

            if (!cacLopDaDangKyIds.Any()) return (false, string.Empty);

            // 3. Lấy toàn bộ lịch học của các lớp đã đăng ký
            var lichDaDangKy = await _context.LichHocs
                .Include(lh => lh.LopHocPhan).ThenInclude(l => l.MonHoc) // Để lấy tên môn báo lỗi
                .Where(lh => cacLopDaDangKyIds.Contains(lh.LopHocPhanId))
                .AsNoTracking()
                .ToListAsync();

            // 4. So sánh trùng lặp từng buổi học
            foreach (var lichMoi in lhpMoi.LichHocs)
            {
                foreach (var lichCu in lichDaDangKy)
                {
                    // Logic trùng: Cùng Ngày VÀ Thời gian giao nhau
                    if (lichMoi.Ngay.Date == lichCu.Ngay.Date)
                    {
                        // Kiểm tra giao nhau: (StartA < EndB) && (EndA > StartB)
                        if (lichMoi.GioBatDau < lichCu.GioKetThuc && lichMoi.GioKetThuc > lichCu.GioBatDau)
                        {
                            string tenMonTrung = lichCu.LopHocPhan?.MonHoc?.TenMonHoc ?? "Môn học khác";
                            string maLopTrung = lichCu.LopHocPhan?.MaLopHocPhan ?? "";

                            string gioMoi = $"{lichMoi.GioBatDau:hh\\:mm}-{lichMoi.GioKetThuc:hh\\:mm}";
                            string gioCu = $"{lichCu.GioBatDau:hh\\:mm}-{lichCu.GioKetThuc:hh\\:mm}";

                            string msg = $"Trùng lịch với môn '{tenMonTrung}' ({maLopTrung}) vào ngày {lichMoi.Ngay:dd/MM/yyyy}. " +
                                         $"Giờ lớp mới: {gioMoi}, Giờ lớp cũ: {gioCu}.";

                            return (true, msg);
                        }
                    }
                }
            }

            return (false, string.Empty);
        }
        #endregion
    }
}