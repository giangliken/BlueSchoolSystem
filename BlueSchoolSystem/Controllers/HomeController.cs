using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace BlueSchoolSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, ApplicationDbContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        //Giao diện trang chủ
        public IActionResult Index()
        {
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
        public async Task<IActionResult> ThoiKhoaBieu(int weekOffset = 0, int monthOffset = 0, string viewMode = "week")
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            // Lấy token từ Session để gọi API
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Lấy MSSV từ Claim (khi login bạn đã set vào claim Identity)
            var mssv = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(mssv))
            {
                ViewBag.Error = "Không xác định được MSSV của người dùng.";
                return View(new List<ThoiKhoaBieuViewModel>());
            }

            // Gọi API lấy TKB của sinh viên
            var response = await client.GetAsync($"api/thoikhoabieusinhvien/{mssv}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể lấy thời khóa biểu từ API.";
                return View(new List<ThoiKhoaBieuViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                ViewBag.Error = "Không tìm thấy dữ liệu thời khóa biểu.";
                return View(new List<ThoiKhoaBieuViewModel>());
            }

            var tkb = JsonSerializer.Deserialize<List<ThoiKhoaBieuViewModel>>(
                dataElement.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // ===== TÍNH NGÀY BẮT ĐẦU TUẦN & THÁNG DỰA VÀO OFFSET =====
            var today = DateTime.Today;

            // Tuần: tính thứ 2 (Monday) của tuần hiện tại
            var monday = today.AddDays(-(int)today.DayOfWeek + 1);
            if (monday.DayOfWeek == DayOfWeek.Sunday) monday = monday.AddDays(-6);
            monday = monday.AddDays(7 * weekOffset);

            // Tháng: lấy ngày 1 của tháng hiện tại
            var monthDate = new DateTime(today.Year, today.Month, 1).AddMonths(monthOffset);

            ViewBag.WeekStart = monday;
            ViewBag.MonthStart = monthDate;
            ViewBag.WeekOffset = weekOffset;
            ViewBag.MonthOffset = monthOffset;
            ViewBag.ViewMode = viewMode;

            return View(tkb ?? new List<ThoiKhoaBieuViewModel>());
        }

        //Giao diện Lịch thi
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> LichThi(string hocKy)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

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
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // ✅ Lấy MSSV từ token
            var mssv = User.Identity?.Name ??
                       User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                       User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(mssv))
            {
                ViewBag.Error = "Không xác định được MSSV của người dùng.";
                return View(new List<DiemMonHocViewModel>());
            }

            // ✅ Lấy ngày nhập học từ DB
            var ngayNhapHoc = await _context.SinhViens
                .Where(s => s.MSSV == mssv)
                .Select(s => s.NgayNhapHoc)
                .FirstOrDefaultAsync();

            // ✅ Lấy danh sách học kỳ
            var hocKyData = await _context.HocKys
                .Where(hk => hk.NgayBatDau >= ngayNhapHoc)
                .OrderByDescending(hk => hk.NgayBatDau)
                .Select(hk => new { hk.Id, hk.TenHocKy, hk.NgayBatDau })
                .ToListAsync();

            // Nếu chưa chọn thì mặc định học kỳ mới nhất
            if (string.IsNullOrEmpty(hocKy) && hocKyData.Any())
            {
                hocKy = hocKyData.First().Id.ToString();
            }

            ViewBag.HocKySelected = int.TryParse(hocKy, out var hkId) ? hkId : 0;
            ViewBag.HocKyList = new SelectList(hocKyData, "Id", "TenHocKy", ViewBag.HocKySelected);

            // ✅ Gọi API lấy toàn bộ điểm sinh viên
            var response = await client.GetAsync($"api/diemsinhvien/{mssv}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể lấy điểm từ API.";
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

            // ✅ Lấy điểm học kỳ đang chọn
            var selectedHocKy = diemResponse.Data
                .FirstOrDefault(d => d.HocKyId == ViewBag.HocKySelected);

            var diemHocKy = selectedHocKy?.Diems ?? new List<DiemMonHocViewModel>();

            // ✅ Set ViewBag thống kê từ API
            ViewBag.DiemTBTichLuy = diemResponse.DiemTBTichLuy;
            ViewBag.TongTinChiDat = diemResponse.TongTinChiDat;
            ViewBag.TongTinChiTichLuy = diemResponse.TongTinChiTichLuy;
            ViewBag.TichLuyList = diemResponse.TichLuyList;

            // ✅ Trả về điểm học kỳ đã chọn để hiển thị
            return View(diemHocKy);
        }


        //Giao diện Lớp học phần
        public IActionResult LopHocPhan()
        {
            return View();
        }

        //Giao diện đăng ký học phần
        public IActionResult DangKyLopHP()
        {
            return View();
        }

        //Giao diện Hoc phí
        public IActionResult HocPhi()
        {
            return View();
        }

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
