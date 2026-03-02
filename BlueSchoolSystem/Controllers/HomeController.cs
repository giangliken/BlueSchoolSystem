using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using BlueSchoolSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BlueSchoolSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly LopHocPhanService _lhpService;
        private readonly HocPhiService _hocPhiService;
        private readonly string? _apiBaseUrl;


        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, ApplicationDbContext context, LopHocPhanService lhpService, HocPhiService hocPhiService, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _lhpService = lhpService;
            _hocPhiService = hocPhiService;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"];
        }

        //Giao diện trang chủ
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var mssv = User.Identity?.Name ??
                       User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                       User.Identity?.Name;

            if (string.IsNullOrEmpty(mssv))
            {
                ViewBag.Error = "Không xác định được MSSV của sinh viên.";
                return View(new List<ThoiKhoaBieuViewModel>());
            }


            List<SuKienViewModel> suKiens = new();

            var sinhVien = await _context.SinhViens
                                         .Include(s => s.Lop) 
                                         .ThenInclude(l => l.Nganh)
                                         .ThenInclude(n => n.Khoa)
                                         .FirstOrDefaultAsync(s => s.MSSV == mssv);

            if (sinhVien == null)
            {
                ViewBag.Error = "Không tìm thấy thông tin khoa của sinh viên.";
                ViewBag.SuKiens = suKiens;
                return View();
            }

            string maKhoa = sinhVien.Lop?.Nganh?.Khoa?.MaKhoa; 

            client.BaseAddress = new Uri(_apiBaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Gọi đúng endpoint bạn vừa viết: api/sukien/sukienkhoa/{maKhoa}
            var response = await client.GetAsync($"api/sukienkhoa/{maKhoa}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                suKiens = JsonConvert.DeserializeObject<List<SuKienViewModel>>(json)
                          ?? new List<SuKienViewModel>();
            }
            else
            {
                ViewBag.Error = "Không thể tải sự kiện của khoa.";
            }

            // ====== 4. THỐNG KÊ (Tuỳ chọn cho sinh viên) ======
            // Sinh viên có thể chỉ cần xem số lượng đơn giản hoặc bỏ qua phần này
            ViewBag.Role = "SinhVien";
            ViewBag.TenKhoa = sinhVien.Lop?.Nganh?.Khoa.TenKhoa; // Để hiển thị tên khoa lên View
            ViewBag.SuKiens = suKiens;
            ViewBag.TenSinhVien = sinhVien.HoVaTenDem + " " + sinhVien.Ten;
            return View();
        }



        //Giao diện đăng nhập
        public IActionResult Login()
        {
            return Redirect("/Identity/Account/Login");
        }

        //Học vụ 
        //Giao diện Thời khóa biểu
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ThoiKhoaBieu( int weekOffset = 0,  int monthOffset = 0,  string viewMode = "week")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl);

                // 🔹 Gắn token từ session vào header Authorization
                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // 🔹 Lấy MSSV từ Claims (ưu tiên claim "username")

                var mssv = User.Identity?.Name ??
                           User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                           User.Identity?.Name;

                if (string.IsNullOrEmpty(mssv))
                {
                    ViewBag.Error = "Không xác định được MSSV của sinh viên.";
                    return View(new List<ThoiKhoaBieuViewModel>());
                }

                // 🔹 Gọi API lấy dữ liệu thời khóa biểu
                var response = await client.GetAsync($"api/thoikhoabieusinhvien/{mssv}");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = $"Không thể lấy dữ liệu thời khóa biểu. (Mã lỗi: {response.StatusCode})";
                    return View(new List<ThoiKhoaBieuViewModel>());
                }

                var json = await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // 🔹 Lấy dữ liệu trong "data"
                if (!root.TryGetProperty("data", out var dataElement))
                {
                    ViewBag.Error = "Không tìm thấy dữ liệu thời khóa biểu trong phản hồi API.";
                    return View(new List<ThoiKhoaBieuViewModel>());
                }

                var tkb = JsonSerializer.Deserialize<List<ThoiKhoaBieuViewModel>>(
                    dataElement.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<ThoiKhoaBieuViewModel>();

                // 🔹 Tính ngày đầu tuần & đầu tháng
                var today = DateTime.Today;

                // Thứ hai đầu tuần
                var monday = today.AddDays(-(int)today.DayOfWeek + 1);
                if (monday.DayOfWeek == DayOfWeek.Sunday) monday = monday.AddDays(-6);
                monday = monday.AddDays(7 * weekOffset);

                // Ngày đầu tháng
                var monthDate = new DateTime(today.Year, today.Month, 1).AddMonths(monthOffset);

                // 🔹 Truyền dữ liệu cho view
                ViewBag.WeekStart = monday;
                ViewBag.MonthStart = monthDate;
                ViewBag.WeekOffset = weekOffset;
                ViewBag.MonthOffset = monthOffset;
                ViewBag.ViewMode = viewMode;

                return View(tkb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải thời khóa biểu sinh viên");
                ViewBag.Error = "Đã xảy ra lỗi khi tải thời khóa biểu.";
                return View(new List<ThoiKhoaBieuViewModel>());
            }
        }


        //Giao diện Lịch thi
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> LichThi(string hocKy)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var mssv = User.Identity?.Name ??
                       User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                       User.Identity?.Name;

            if (string.IsNullOrEmpty(mssv))
            {
                ViewBag.Error = "Không xác định được MSSV của người dùng.";
                return View(new List<LichThiViewModel>());
            }

            // Lấy ngày nhập học từ DB
            var ngayNhapHoc = await _context.SinhViens
                .Where(s => s.MSSV == mssv)
                .Select(s => s.NgayNhapHoc)
                .FirstOrDefaultAsync();

            // Lấy danh sách học kỳ từ bảng HocKys (lọc >= ngày nhập học)
            var hocKyData = await _context.HocKys
                .Where(hk => hk.NgayBatDau >= ngayNhapHoc)
                .OrderByDescending(hk => hk.NgayBatDau)
                .Select(hk => new
                {
                    hk.Id,
                    hk.TenHocKy,
                    hk.NgayBatDau
                })
                .ToListAsync();

            // Nếu chưa chọn thì mặc định chọn học kỳ mới nhất
            if (string.IsNullOrEmpty(hocKy) && hocKyData.Any())
            {
                hocKy = hocKyData.First().Id.ToString();
            }

            // Gán học kỳ đã chọn
            ViewBag.HocKySelected = int.TryParse(hocKy, out var hkId) ? hkId : 0;

            // Dropdown từ bảng HocKys
            ViewBag.HocKyList = new SelectList(hocKyData, "Id", "TenHocKy", ViewBag.HocKySelected);

            // Gọi API lấy lịch thi của sinh viên
            var response = await client.GetAsync($"api/lichthisinhvien/{mssv}?hocKyId={ViewBag.HocKySelected}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể lấy lịch thi từ API.";
                return View(new List<LichThiViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                ViewBag.Error = "Không tìm thấy dữ liệu lịch thi.";
                return View(new List<LichThiViewModel>());
            }

            // Deserialize lịch thi
            List<LichThiViewModel> lichThi = new();

            if (dataElement.ValueKind == JsonValueKind.Array)
            {
                // Tìm học kỳ đã chọn trong mảng data
                var selectedHocKy = dataElement
                    .EnumerateArray()
                    .FirstOrDefault(hk => hk.GetProperty("hocKyId").GetInt32() == ViewBag.HocKySelected);

                if (selectedHocKy.ValueKind != JsonValueKind.Undefined &&
                    selectedHocKy.TryGetProperty("lichThis", out var lichThisElement) &&
                    lichThisElement.ValueKind == JsonValueKind.Array)
                {
                    lichThi = JsonSerializer.Deserialize<List<LichThiViewModel>>(
                        lichThisElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new List<LichThiViewModel>();
                }
            }

            return View(lichThi);
        }


        // Giao diện Xem điểm
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> XemDiem(string hocKy)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var mssv = User.Identity?.Name ??
                       User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                       User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(mssv))
            {
                ViewBag.Error = "Không xác định được MSSV của người dùng.";
                return View(new List<DiemMonHocViewModel>());
            }

            var response = await client.GetAsync($"api/diemsinhvien/{mssv}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không có dữ liệu điểm trong Cơ sở dữ liệu";
                return View(new List<DiemMonHocViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            var diemResponse = JsonSerializer.Deserialize<DiemSinhVienResponseViewModel>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (diemResponse == null || diemResponse.Data == null || !diemResponse.Data.Any())
            {
                ViewBag.Error = "Không tìm thấy dữ liệu điểm.";
                return View(new List<DiemMonHocViewModel>());
            }

            // --- danh sách học kỳ (tăng dần theo NgayBatDau)
            var hocKyData = diemResponse.Data
                .Select(d => new { d.HocKyId, d.TenHocKy, d.NgayBatDau })
                .OrderByDescending(d => d.NgayBatDau)
                .ToList();

            // mặc định học kỳ mới nhất 
            if (string.IsNullOrEmpty(hocKy) && hocKyData.Any())
                hocKy = hocKyData.First().HocKyId.ToString();

            var selectedId = int.TryParse(hocKy, out var hkId) ? hkId : 0;
            ViewBag.HocKySelected = selectedId;

            // build select list và thêm "Tất cả" ở cuối
            var selectItems = hocKyData
                .Select(d => new { Value = d.HocKyId.ToString(), Text = d.TenHocKy })
                .ToList();
            selectItems.Add(new { Value = "0", Text = "Tất cả" });
            ViewBag.HocKyList = new SelectList(selectItems, "Value", "Text", selectedId.ToString());

            // nếu chọn "Tất cả" -> build ViewBag.DiemTatCaHocKy (mỗi phần tử có thêm thống kê từ TichLuyList)
            if (selectedId == 0)
            {
                var tichLuyList = diemResponse.TichLuyList ?? new List<TichLuyHocKyViewModel>();

                var tatCaHocKy = diemResponse.Data
                    .OrderBy(d => d.NgayBatDau)
                    .Select(d => new
                    {
                        d.HocKyId,
                        d.TenHocKy,
                        d.NgayBatDau,
                        Diems = d.Diems,
                        DiemTBHocKy = tichLuyList.FirstOrDefault(t => t.TenHocKy == d.TenHocKy)?.DiemTBHocKy ?? 0.0,
                        DiemTBTichLuy = tichLuyList.FirstOrDefault(t => t.TenHocKy == d.TenHocKy)?.DiemTBTichLuy ?? 0.0,
                        TinChiDat = tichLuyList.FirstOrDefault(t => t.TenHocKy == d.TenHocKy)?.TinChiDat ?? 0,
                        TongTinChiTichLuy = tichLuyList.FirstOrDefault(t => t.TenHocKy == d.TenHocKy)?.TongTinChiTichLuy ?? 0
                    })
                    .ToList();

                ViewBag.DiemTatCaHocKy = tatCaHocKy;
                ViewBag.TichLuyList = diemResponse.TichLuyList;

                // trả model rỗng vì view sẽ dùng ViewBag.DiemTatCaHocKy để render nhiều bảng
                return View(new List<DiemMonHocViewModel>());
            }

            // Nếu chỉ 1 học kỳ
            var selectedHocKy = diemResponse.Data.FirstOrDefault(d => d.HocKyId == selectedId);
            var model = selectedHocKy?.Diems ?? new List<DiemMonHocViewModel>();
            ViewBag.SelectedHocKyName = selectedHocKy?.TenHocKy;

            var tichLuyHienTai = diemResponse.TichLuyList?.FirstOrDefault(t => t.TenHocKy == selectedHocKy?.TenHocKy);
            if (tichLuyHienTai != null)
            {
                ViewBag.DiemTBHocKy = tichLuyHienTai.DiemTBHocKy;
                ViewBag.DiemTBTichLuy = tichLuyHienTai.DiemTBTichLuy;
                ViewBag.TongTinChiTichLuy = tichLuyHienTai.TongTinChiTichLuy;
                ViewBag.TongTinChiDat = tichLuyHienTai.TinChiDat;
            }

            ViewBag.TichLuyList = diemResponse.TichLuyList;
            return View(model);
        }




        // Giao diện Lớp học phần
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> LopHocPhan(string hocKy)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var mssv = User.Identity?.Name ??
                       User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                       User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(mssv))
            {
                ViewBag.Error = "Không xác định được MSSV của người dùng.";
                return View(new List<LopHocPhanHocKyViewModel>()); // ✅ fix: đúng type
            }

            var response = await client.GetAsync($"api/lophocphansinhvien/{mssv}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không có dữ liệu lớp học phần trong Cơ sở dữ liệu";
                return View(new List<LopHocPhanHocKyViewModel>()); // ✅ fix: đúng type
            }

            var body = await response.Content.ReadAsStringAsync();
            var lopResponse = JsonSerializer.Deserialize<LopHocPhanResponseViewModel>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (lopResponse == null || lopResponse.Data == null || !lopResponse.Data.Any())
            {
                ViewBag.Error = "Không tìm thấy dữ liệu lớp học phần.";
                return View(new List<LopHocPhanHocKyViewModel>()); // ✅ fix: đúng type
            }

            // Lấy 3 học kỳ mới nhất từ API
            var model = lopResponse.Data?
                .OrderByDescending(d => d.NgayBatDau)
                .Take(3)
                .Select(hk => new LopHocPhanHocKyViewModel
                {
                    HocKyId = hk.HocKyId,
                    TenHocKy = hk.TenHocKy,
                    NgayBatDau = hk.NgayBatDau,
                    DanhSachMon = hk.DanhSachMon ?? new List<LopHocPhanMonHocViewModel>()
                }).ToList() ?? new List<LopHocPhanHocKyViewModel>();

            return View(model);
        }

        // Hiện buổi điểm danh
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> BuoiDiemDanh(int lopHocPhanId, string tenMonHoc, string maLopHocPhan)
        {
            ViewBag.TenMonHoc = tenMonHoc;
            ViewBag.LopHocPhanId = lopHocPhanId;
            ViewBag.MaLopHocPhan = maLopHocPhan;

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            // Lấy access token từ session
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Lấy MSSV từ claims
            var mssv = User.Identity?.Name
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(mssv))
            {
                ViewBag.Error = "Không xác định được MSSV của người dùng.";
                return View(new List<DiemDanhViewModel>());
            }

            // Gọi API backend
            var apiUrl = $"api/lophocphansinhvien/{mssv}/lop/{lopHocPhanId}/diemdanh";
            var response = await client.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không có dữ liệu điểm danh.";
                return View(new List<DiemDanhViewModel>());
            }

            // Deserialize dữ liệu
            var body = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<DiemDanhResponse<List<DiemDanhViewModel>>>(
                 body,
                 new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
             );

            var model = apiResponse?.Data ?? new List<DiemDanhViewModel>();

            return View(model);
        }


        //Giao diện đăng ký học phần
        #region ======= Đăng ký học phần =======

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DangKyLopHP()
        {
            // A. Lấy thông tin sinh viên
            var mssv = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sv = await _context.SinhViens.FirstOrDefaultAsync(s => s.MSSV == mssv);
            if (sv == null) return RedirectToAction("Login", "Account");

            // B. Tự động lấy Học kỳ mới nhất
            var hocKyMoiNhat = await _context.HocKys
                .OrderByDescending(h => h.NgayBatDau)
                .FirstOrDefaultAsync();

            if (hocKyMoiNhat == null)
            {
                ViewBag.Error = "Hiện tại chưa có học kỳ nào được mở đăng ký.";
                return View(new List<DangKyHocPhanVM>());
            }

            ViewBag.TenHocKy = hocKyMoiNhat.TenHocKy;

            // C. Lấy danh sách Lớp học phần
            var listLopRaw = await _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.GiangVien)
                .Include(l => l.TrangThai)
                .Include(l => l.LichHocs).ThenInclude(lh => lh.PhongHoc)
                .Where(l => l.HocKyId == hocKyMoiNhat.Id)
                .Where(l => l.TrangThai.TenTrangThai == "Đang mở")
                .ToListAsync();

            // D. Lấy danh sách đã đăng ký
            var daDangKyIds = await _context.DangKyHocPhans
                .Where(dk => dk.SinhVienId == sv.Id && dk.LopHocPhan.HocKyId == hocKyMoiNhat.Id)
                .Select(dk => dk.LopHocPhanId)
                .ToListAsync();

            // E. Map sang ViewModel (QUAN TRỌNG: Tách lịch học chi tiết)
            var model = new List<DangKyHocPhanVM>();

            foreach (var lhp in listLopRaw)
            {
                int soLuongDaDK = await _context.DangKyHocPhans.CountAsync(dk => dk.LopHocPhanId == lhp.Id);

                // --- XỬ LÝ TÁCH LỊCH HỌC ---
                var lichList = new List<LichHocChiTietVM>();
                if (lhp.LichHocs != null && lhp.LichHocs.Any())
                {
                    var uniqueSchedules = lhp.LichHocs
                        .GroupBy(lh => new { lh.Ngay.DayOfWeek, lh.GioBatDau, lh.GioKetThuc, Phong = lh.PhongHoc.MaPhongHoc })
                        .Select(g => g.Key)
                        .OrderBy(g => g.DayOfWeek)
                        .ToList();

                    var mapStart = _lhpService.GetTietStartMap();
                    var mapEnd = _lhpService.GetTietEndMap();

                    foreach (var item in uniqueSchedules)
                    {
                        string thu = item.DayOfWeek == DayOfWeek.Sunday ? "CN" : "T" + ((int)item.DayOfWeek + 1);

                        int tietBD = mapStart.FirstOrDefault(x => Math.Abs((TimeSpan.Parse(x.Value) - item.GioBatDau).TotalMinutes) < 2).Key;
                        int tietKT = mapEnd.FirstOrDefault(x => Math.Abs((TimeSpan.Parse(x.Value) - item.GioKetThuc).TotalMinutes) < 2).Key;
                        int soTiet = (tietBD > 0 && tietKT > 0) ? (tietKT - tietBD + 1) : 0;

                        lichList.Add(new LichHocChiTietVM
                        {
                            Thu = thu,
                            TietBatDau = tietBD > 0 ? tietBD : 0,
                            SoTiet = soTiet,
                            Phong = item.Phong
                        });
                    }
                }

                // Fallback nếu chưa có lịch
                if (!lichList.Any()) lichList.Add(new LichHocChiTietVM { Thu = "-", Phong = "Chưa xếp" });

                model.Add(new DangKyHocPhanVM
                {
                    Id = lhp.Id,
                    MaLopHocPhan = lhp.MaLopHocPhan,
                    MaMonHoc = lhp.MonHoc?.MaMonHoc ?? "",
                    TenMonHoc = lhp.MonHoc?.TenMonHoc ?? "",
                    SoTinChi = lhp.MonHoc?.SoTinChi ?? 0,
                    GiangVien = lhp.GiangVien?.MaGiangVien ?? "Chưa gán",
                    NgayBatDau = lhp.NgayBatDau,
                    NgayKetThuc = lhp.NgayKetThuc,
                    SiSo = lhp.SiSo,
                    DaDangKy = soLuongDaDK,
                    IsRegistered = daDangKyIds.Contains(lhp.Id),
                    LichHocChiTiet = lichList // Gán list chi tiết
                });
            }

            return View(model);
        }

        // 2. Xử lý Đăng ký / Hủy đăng ký (AJAX POST)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> XuLyDangKy(int lopHocPhanId, bool isDangKy)
        {
            try
            {
                var mssv = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sv = await _context.SinhViens.FirstOrDefaultAsync(s => s.MSSV == mssv);
                if (sv == null) return Json(new { success = false, message = "Không tìm thấy thông tin sinh viên." });

                var lhp = await _context.LopHocPhans.Include(l => l.MonHoc).FirstOrDefaultAsync(l => l.Id == lopHocPhanId);
                if (lhp == null) return Json(new { success = false, message = "Lớp học phần không tồn tại." });

                // ================= LOGIC ĐĂNG KÝ =================
                if (isDangKy)
                {
                    // 1. Kiểm tra đã đăng ký chưa
                    bool exists = await _context.DangKyHocPhans.AnyAsync(dk => dk.SinhVienId == sv.Id && dk.LopHocPhanId == lopHocPhanId);
                    if (exists) return Json(new { success = false, message = "Bạn đã đăng ký lớp này rồi." });

                    // 2. Kiểm tra trùng môn (trong cùng học kỳ)
                    bool trungMon = await _context.DangKyHocPhans
                        .Include(dk => dk.LopHocPhan)
                        .AnyAsync(dk => dk.SinhVienId == sv.Id
                                     && dk.LopHocPhan.HocKyId == lhp.HocKyId
                                     && dk.LopHocPhan.MonHocId == lhp.MonHocId);
                    if (trungMon) return Json(new { success = false, message = $"Bạn đã đăng ký môn {lhp.MonHoc?.TenMonHoc} ở lớp khác trong kỳ này." });

                    // 3. [QUAN TRỌNG] Kiểm tra trùng lịch học
                    // Gọi Service CheckTrungLichSinhVienAsync đã viết trước đó
                    var checkLich = await _lhpService.CheckTrungLichSinhVienAsync(sv.Id, lopHocPhanId);
                    if (checkLich.isConflict)
                    {
                        return Json(new { success = false, message = $"Trùng lịch: {checkLich.conflictDetails}" });
                    }

                    // 4. Kiểm tra sĩ số
                    int currentCount = await _context.DangKyHocPhans.CountAsync(dk => dk.LopHocPhanId == lopHocPhanId);
                    if (currentCount >= lhp.SiSo) return Json(new { success = false, message = "Lớp đã đầy sĩ số." });

                    // --- A. THÊM VÀO BẢNG ĐĂNG KÝ ---
                    var dkMoi = new DangKyHocPhan
                    {
                        SinhVienId = sv.Id,
                        LopHocPhanId = lopHocPhanId,
                        NgayDangKy = DateTime.Now,
                        LoaiDangKy = "TuChon"
                    };
                    _context.DangKyHocPhans.Add(dkMoi);

                    // --- B. THÊM VÀO CHI TIẾT LỚP HỌC PHẦN ---
                    _context.ChiTietLopHocPhans.Add(new ChiTietLopHocPhan
                    {
                        LopHocPhanId = lopHocPhanId,
                        SinhVienId = sv.Id
                    });

                    // Lưu DB để có ID
                    await _context.SaveChangesAsync();

                    // --- C. TÍNH TIỀN ---
                    try
                    {
                        await _hocPhiService.GhiNoHocPhiAsync(dkMoi.Id);
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng không chặn luồng
                    }

                    return Json(new { success = true, message = "Đăng ký thành công!", daDangKy = currentCount + 1 });
                }
                // ================= LOGIC HỦY ĐĂNG KÝ =================
                else
                {
                    var dkToDelete = await _context.DangKyHocPhans
                        .FirstOrDefaultAsync(dk => dk.SinhVienId == sv.Id && dk.LopHocPhanId == lopHocPhanId);

                    if (dkToDelete == null) return Json(new { success = false, message = "Bạn chưa đăng ký lớp này." });

                    // 1. Trừ tiền trước
                    try
                    {
                        await _hocPhiService.HuyNoHocPhiAsync(dkToDelete.Id);
                    }
                    catch (Exception ex) { }

                    // 2. Xóa Đăng ký & Chi tiết
                    _context.DangKyHocPhans.Remove(dkToDelete);

                    var chiTietToDelete = await _context.ChiTietLopHocPhans
                        .FirstOrDefaultAsync(ct => ct.SinhVienId == sv.Id && ct.LopHocPhanId == lopHocPhanId);
                    if (chiTietToDelete != null) _context.ChiTietLopHocPhans.Remove(chiTietToDelete);

                    await _context.SaveChangesAsync();

                    int currentCount = await _context.DangKyHocPhans.CountAsync(dk => dk.LopHocPhanId == lopHocPhanId);
                    return Json(new { success = true, message = "Hủy đăng ký thành công!", daDangKy = currentCount });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        #endregion

        //Giao diện Hoc phí
        #region ======= HỌC PHÍ SINH VIÊN =======


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> HocPhi()
        {
            var mssv = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sv = await _context.SinhViens.FirstOrDefaultAsync(s => s.MSSV == mssv);

            if (sv == null) return RedirectToAction("Login", "Account");

            // 1. Gọi Service lấy toàn bộ thông tin
            var model = await _hocPhiService.GetThongTinHocPhi(sv.Id);

            // 2. Xử lý lọc: Lấy học kỳ mới nhất dựa trên NGÀY BẮT ĐẦU (Time) thay vì ID
            if (model.DanhSachHocKy != null && model.DanhSachHocKy.Any())
            {
                // B1: Lấy danh sách các ID học kỳ mà sinh viên có dữ liệu
                var existingHocKyIds = model.DanhSachHocKy.Select(k => k.HocKyId).ToList();

                // B2: Truy vấn DB để tìm ID có NgayBatDau mới nhất trong số các ID trên
                var latestHocKyId = await _context.HocKys
                    .Where(h => existingHocKyIds.Contains(h.Id)) // Chỉ xét các kỳ SV có tham gia
                    .OrderByDescending(h => h.NgayBatDau)       // Sắp xếp theo thời gian thực tế
                    .Select(h => h.Id)
                    .FirstOrDefaultAsync();

                // B3: Lọc model chỉ giữ lại học kỳ đó
                if (latestHocKyId > 0)
                {
                    var kyMoiNhat = model.DanhSachHocKy.FirstOrDefault(k => k.HocKyId == latestHocKyId);
                    if (kyMoiNhat != null)
                    {
                        model.DanhSachHocKy = new List<HocPhiTheoKyVM> { kyMoiNhat };
                    }
                }
            }

            return View(model);
        }

        #endregion

        //Yêu cầu - Trợ giúp
        //Giao diện Xác nhận online
        public IActionResult XacNhanOnline()
        {
            return View();
        }
        //Giao diện Nộp đơn
        public IActionResult NopDon()
        {
            return View();
        }

        //Hỏi - Đáp
        //Giao diện Tạo câu hỏi mới
        public IActionResult TaoCauHoiMoi()
        {
            return View();
        }
        //Giao diện Câu hỏi đã gửi
        public IActionResult CauHoiDaGui()
        {
            return View();
        }


        //Đánh giá rèn luyện
        //Giao diện Điểm cá nhân
        public IActionResult DanhGiaDiemRenLuyenCaNhan()
        {
            return View();
        }
        //Đoàn - Hội
        //Giao diện Hồ sơ
        public IActionResult HoSo()
        {
            return View();
        }
        //Giao diện Hoạt động
        public IActionResult HoatDong()
        {
            return View();
        }
        //Giao diện Tài liệu
        public IActionResult TaiLieu()
        {
            return View();
        }

        //Khảo sát
        //Giao diện Hoạt động giảng dạy
        public IActionResult HoatDongGiangDay()
        {
            return View();
        }
        //Khác
        //Giao diện Danh bạ
        public IActionResult DanhBa()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
