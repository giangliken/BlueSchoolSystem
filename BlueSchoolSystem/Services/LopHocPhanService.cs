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

        public LopHocPhanService(ApplicationDbContext context, ILogger<LopHocPhanService> logger)
        {
            _context = context;
            _logger = logger;
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

            int tietBatDau = isMorning ? _random.Next(2, 4) : _random.Next(7, 10);
            int tietKetThuc = isMorning ? _random.Next(4, 7) : _random.Next(10, 13);

            // Đảm bảo kết thúc không trước bắt đầu và nằm trong ca.
            if (tietKetThuc <= tietBatDau)
            {
                tietKetThuc = isMorning ? 6 : 12;
                if (tietBatDau == tietKetThuc) tietBatDau = isMorning ? 2 : 7;
            }

            int dayOfWeek = _random.Next(2, 8); // Thứ 2 (2) đến Thứ 7 (7)

            return (tietBatDau, tietKetThuc, dayOfWeek);
        }

        // --- HELPER MỚI: Tạo danh sách các buổi học ---
        private List<LichHocDTO> GenerateWeeklySchedule(DateTime ngayBatDau, DateTime ngayKetThuc,
                                                     int dayOfWeekCustom, int startTiet, int endTiet, int phongHocId, bool isThucHanh)
        {
            var schedules = new List<LichHocDTO>();
            var (gioBatDau, gioKetThuc) = ConvertTietToTimeSpan(startTiet, endTiet);
            DayOfWeek targetDay = dayOfWeekCustom == 8 ? DayOfWeek.Sunday : (DayOfWeek)(dayOfWeekCustom - 1);

            DateTime current = ngayBatDau;
            while (current.DayOfWeek != targetDay)
            {
                current = current.AddDays(1);
            }

            int weekIndex = 0;
            while (current <= ngayKetThuc)
            {
                if (!isThucHanh || weekIndex % 2 == 0) // LT học mọi tuần; TH học tuần chẵn (index 0, 2, 4...)
                {
                    schedules.Add(new LichHocDTO
                    {
                        LopHocPhanId = 0,
                        Ngay = current.Date,
                        GioBatDau = gioBatDau,
                        GioKetThuc = gioKetThuc,
                        PhongHocId = phongHocId
                    });
                }
                current = current.AddDays(7);
                weekIndex++;
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

        // PHƯƠNG THỨC MỚI: Kiểm tra môn tiên quyết dựa trên ĐĂNG KÝ (cho kỳ hiện tại)
        private async Task<bool> CanStudentEnrollBasedOnPrerequisiteAsync(
            int sinhVienId, int hocKyId, string maMonHocTienQuyet)
        {
            if (string.IsNullOrWhiteSpace(maMonHocTienQuyet))
            {
                return true; // Không có môn tiên quyết
            }

            // B1: KIỂM TRA ĐÃ HOÀN THÀNH TỪ CÁC KỲ TRƯỚC (Dùng Bảng Điểm)
            var completed = await HasStudentCompletedSubjectAsync(sinhVienId, maMonHocTienQuyet);
            if (completed)
            {
                return true;
            }

            // B2: KIỂM TRA ĐÃ ĐĂNG KÝ TRONG KỲ HIỆN TẠI (Dùng DangKyHocPhan)
            // Đây là bước xử lý trường hợp môn lý thuyết và thực hành đi kèm nhau
            var isRegisteredInCurrentSemester = await _context.DangKyHocPhans
                .Include(dk => dk.LopHocPhan)
                    .ThenInclude(lhp => lhp.MonHoc)
                .AnyAsync(dk => dk.SinhVienId == sinhVienId &&
                                dk.LopHocPhan.HocKyId == hocKyId &&
                                dk.LopHocPhan.MonHoc.MaMonHoc == maMonHocTienQuyet);

            if (isRegisteredInCurrentSemester)
            {
                return true;
            }

            // Nếu không hoàn thành ở kỳ trước, cũng không đăng ký trong kỳ này
            return false;
        }



        public async Task<(int classesCreated, int successCount, int schedulesCreated)> RunAutoEnrollmentJobAsync(AutoEnrollmentApiPayload dto)
        {
            // 1. XÁC ĐỊNH KHÓA HỌC VÀ SINH VIÊN MỤC TIÊU (Giữ nguyên)
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
            const int MAX_LHP_SIZE = 120;
            const int MAX_STUDENTS_PER_CLASS_IN_LHP = 50;
            var trangThaiChoMoId = _context.TrangThais.FirstOrDefault(t => t.LoaiTrangThai == "LopHocPhan" && t.TenTrangThai == "Chờ mở")?.Id ?? 1;
            var hocKy = await _context.HocKys.FindAsync(dto.HocKyId);

            int successCount = 0;
            int classesCreated = 0;
            int schedulesCreatedTotal = 0;
            var createdLhpsBySubject = new Dictionary<string, List<LopHocPhan>>();

            // Lấy tham số lịch học từ DTO (đã được Controller gán giá trị random/default)
            int autoTietBatDau = dto.TietBatDau;
            int autoTietKetThuc = dto.TietKetThuc;
            int autoPhongHocId = dto.DefaultPhongHocId;
            var availableDays = dto.CacNgayTrongTuan.ToList();

            // 3. VÒNG LẶP XỬ LÝ TỪNG MÔN HỌC
            foreach (var maMonHoc in dto.MandatorySubjectCodes)
            {
                var monHoc = await _context.MonHocs.FirstOrDefaultAsync(m => m.MaMonHoc == maMonHoc);
                if (monHoc == null) continue;

                var chiTietCtdt = await GetChiTietCTDTAsync(maMonHoc, dto.NganhId, dto.KhoaNhapHoc, dto.ThuTuHocKy);

                // Xác định loại môn học để giới hạn số buổi
                bool isThucHanh = monHoc.MoTa?.ToUpper().Contains("TH") ?? false;
                int maxAllowedSessions = isThucHanh ? 6 : 9;

                // 1. TÌM GIẢNG VIÊN TỰ ĐỘNG KHẢ DỤNG CHO MÔN HỌC
                var gvCandidates = await GetGiangVienByMonHocAsync(monHoc.Id);
                int? autoGiangVienId = GetRandomGiangVienId(gvCandidates);

                if (!autoGiangVienId.HasValue) continue;

                // Lấy ngày random từ list đã được gửi lên
                int randomIndex = _random.Next(dto.CacNgayTrongTuan.Count);
                int autoDayOfWeek = dto.CacNgayTrongTuan[randomIndex];

                // Khởi tạo list cache
                if (!createdLhpsBySubject.ContainsKey(maMonHoc))
                {
                    createdLhpsBySubject[maMonHoc] = new List<LopHocPhan>();
                }

                // Lọc sinh viên chưa học môn này
                var studentsToEnroll = new List<SinhVien>();
                foreach (var sv in targetStudents)
                {
                    if (!await HasStudentCompletedSubjectAsync(sv.Id, maMonHoc))
                    {
                        studentsToEnroll.Add(sv);
                    }
                }

                if (!studentsToEnroll.Any()) continue;

                // --- TÌM SLOT TRỐNG VỚI GV VÀ PHÒNG CỐ ĐỊNH (LOGIC MỚI) ---
                int finalDayOfWeek = 0;
                bool foundValidSlot = false;

                // tìm slot mới mỗi lần chuẩn bị tạo lớp
                var daysToSearch = availableDays.OrderBy(x => Guid.NewGuid()).ToList();
                foreach (var day in daysToSearch)
                {
                    var schedulesToTest = GenerateWeeklySchedule(
                        hocKy.NgayBatDau, hocKy.NgayKetThuc,
                        day, autoTietBatDau, autoTietKetThuc,
                        autoPhongHocId, isThucHanh
                    ).Take(maxAllowedSessions).ToList();

                    var conflicts = await CheckLichTrungAsync(schedulesToTest, autoGiangVienId.Value, 0);

                    if (!conflicts.Any())
                    {
                        finalDayOfWeek = day;
                        foundValidSlot = true;
                        break;
                    }
                }

                if (!foundValidSlot)
                {
                    _logger.LogWarning($"Không thể tạo lớp mới cho môn {maMonHoc} vì hết slot.");
                    continue;
                }
                // Lặp qua tất cả sinh viên cần đăng ký, cố gắng phân bổ
                foreach (var sv in studentsToEnroll)
                {
                    LopHocPhan selectedLhp = null;

                    // Kiểm tra Tiên Quyết
                    if (chiTietCtdt != null && !string.IsNullOrWhiteSpace(chiTietCtdt.MaMonHocTienQuyet))
                    {
                        var canEnroll = await CanStudentEnrollBasedOnPrerequisiteAsync(
                            sv.Id, dto.HocKyId, chiTietCtdt.MaMonHocTienQuyet);
                        if (!canEnroll) continue;
                    }



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

                    // B. Tạo LHP Mới nếu không tìm thấy
                    if (selectedLhp == null)
                    {
                        var maLopHocPhan = await GenerateAutoMaLHPAsync(dto.HocKyId, monHoc.Id);
                        selectedLhp = new LopHocPhan
                        {
                            HocKyId = dto.HocKyId,
                            MonHocId = monHoc.Id,
                            GiangVienId = autoGiangVienId,
                            TrangThaiId = trangThaiChoMoId,
                            MaLopHocPhan = maLopHocPhan,
                            TenLopHocPhan = $"{monHoc.TenMonHoc} (Thứ {autoDayOfWeek})", // Gán Thứ đã random
                            SiSo = MAX_LHP_SIZE,
                            NgayBatDau = hocKy.NgayBatDau,
                            NgayKetThuc = hocKy.NgayKetThuc
                        };

                        _context.LopHocPhans.Add(selectedLhp);
                        await _context.SaveChangesAsync();
                        createdLhpsBySubject[maMonHoc].Add(selectedLhp);
                        classesCreated++;

                        // --- TẠO LỊCH HỌC TỰ ĐỘNG CHO LỚP MỚI ---
                        if (dto.ShouldAutoCreateSchedule)
                        {
                            // [SỬA ĐỔI 3: DÙNG finalDayOfWeek ĐÃ TÌM ĐƯỢC CHO VIỆC TẠO LỊCH]
                            var finalSchedules = GenerateWeeklySchedule(
                                hocKy.NgayBatDau, hocKy.NgayKetThuc,
                                finalDayOfWeek, autoTietBatDau, autoTietKetThuc,
                                autoPhongHocId, isThucHanh);

                            var limitedSchedules = finalSchedules.Take(maxAllowedSessions).ToList();

                            // KHÔNG cần kiểm tra trùng lịch (CheckLichTrungAsync) lần nữa, 
                            // vì chúng ta đã đảm bảo slot đó trống ở bước trên!

                            foreach (var lich in limitedSchedules)
                            {
                                // [LỖI TRÙNG LẶP ĐÃ XẢY RA Ở ĐÂY TRƯỚC ĐÓ]
                                // Logic đơn giản là thêm lịch vào DB
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
                    }

                    // C. Đăng ký sinh viên vào LHP đã chọn/tạo
                    var alreadyRegistered = await _context.DangKyHocPhans
                        .Include(dk => dk.LopHocPhan)
                        .AnyAsync(dk => dk.SinhVienId == sv.Id
                                     && dk.LopHocPhan.MonHocId == monHoc.Id
                                     && dk.LopHocPhan.HocKyId == dto.HocKyId);

                    if (!alreadyRegistered)
                    {
                        _context.DangKyHocPhans.Add(new DangKyHocPhan
                        {
                            SinhVienId = sv.Id,
                            LopHocPhanId = selectedLhp.Id,
                            NgayDangKy = DateTime.Now,
                            LoaiDangKy = "BatBuocAuto"
                        });
                        successCount++;
                    }
                }
            }

            await _context.SaveChangesAsync();
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

            // 2. Lấy thông tin LHP đã chọn và kiểm tra tính hợp lệ
            // Lấy chi tiết LHP (cần MonHoc để check xem có bị trùng DotDangKy LHP khác không)
            var selectedLhps = await _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.TrangThai)
                .Where(l => dto.LopHocPhanIds.Contains(l.Id) && l.HocKyId == dto.HocKyId)
                .ToListAsync();

            // Lọc ra các LHP không tồn tại hoặc không thuộc Học kỳ đang chọn
            if (selectedLhps.Count != dto.LopHocPhanIds.Count)
            {
                var foundIds = selectedLhps.Select(l => l.Id).ToList();
                var missingIds = dto.LopHocPhanIds.Except(foundIds).ToList();
                return (false, $"Các Lớp Học Phần ID sau không hợp lệ/không thuộc Học kỳ {dto.HocKyId}: {string.Join(", ", missingIds)}.");
            }

            // Kiểm tra LHP còn đủ điều kiện mở đăng ký (Đang mở và còn chỗ)
            var trangThaiDangMo = selectedLhps.FirstOrDefault()?.TrangThai?.TenTrangThai ?? "N/A";
            if (trangThaiDangMo != "Đang mở")
            {
                return (false, "Chỉ có thể mở đăng ký cho các Lớp Học Phần đang ở trạng thái 'Đang mở'.");
            }

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
    }
}