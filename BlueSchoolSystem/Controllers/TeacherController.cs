using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Firebase.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QRCoder;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static BlueSchoolSystem.APIControllers.APIGiangVienConTroller;

namespace BlueSchoolSystem.Controllers
{
    public class TeacherController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _logService;
        private readonly string? _apiBaseUrl;

        public TeacherController(IHttpClientFactory httpClientFactory, ApplicationDbContext context,IActivityLogService logService, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _logService = logService;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        }

        //Giao diện trang chủ
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var magv = User.Identity?.Name ??
                       User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                       User.Identity?.Name;


            List<SuKienViewModel> suKiens = new();

            var giangVien = await _context.GiangViens
                .Include(gv => gv.Khoa)
                .FirstOrDefaultAsync(gv => gv.MaGiangVien == magv);


            if (giangVien == null)
            {
                ViewBag.Error = "Không tìm thấy thông tin khoa của sinh viên.";
                ViewBag.SuKiens = suKiens;
                return View();
            }

            string maKhoa = giangVien.Khoa.MaKhoa;

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


            ViewBag.Role = "Teacher";
            ViewBag.TenKhoa = giangVien.Khoa.TenKhoa;
            ViewBag.SuKiens = suKiens;
            ViewBag.TenGiangVien = giangVien.HoVaTenDem + " " + giangVien.Ten;
            return View();
        }

        //Trang xem lịch giảng dạy
        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> LichGiangDay(
            string maGV,
            int weekOffset = 0,
            int monthOffset = 0,
            string viewMode = "week")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://localhost:5001/");

                // 🔥 Lấy token từ session
                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                // 🔥 Nếu maGV không truyền → lấy từ Claims
                maGV ??= User.Identity?.Name;

                if (string.IsNullOrEmpty(maGV))
                {
                    ViewBag.Error = "Không xác định được mã giảng viên.";
                    return View(new List<LichGiangDayViewModel>());
                }

                // 🔥 Call API
                var response = await client.GetAsync($"api/lichgiangday/{maGV}");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = $"Không thể lấy dữ liệu lịch giảng dạy. Mã lỗi: {response.StatusCode}";
                    return View(new List<LichGiangDayViewModel>());
                }

                var json = await response.Content.ReadAsStringAsync();

                // Parse JSON gốc
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("data", out var dataEle))
                {
                    ViewBag.Error = "Phản hồi API không chứa trường 'data'.";
                    return View(new List<LichGiangDayViewModel>());
                }

                // 🔥 Deserialize về model mới
                var lich = System.Text.Json.JsonSerializer.Deserialize<List<LichGiangDayViewModel>>(
                    dataEle.GetRawText(),
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                // ---------- Logic tính tuần & tháng ----------
                var today = DateTime.Today;

                // Lấy thứ 2 của tuần hiện tại
                var monday = today.AddDays(-(int)today.DayOfWeek + 1);
                if (monday.DayOfWeek == DayOfWeek.Sunday)
                    monday = monday.AddDays(-6);

                monday = monday.AddDays(7 * weekOffset);

                var monthDate = new DateTime(today.Year, today.Month, 1)
                    .AddMonths(monthOffset);

                // Gửi xuống View
                ViewBag.WeekStart = monday;
                ViewBag.MonthStart = monthDate;
                ViewBag.WeekOffset = weekOffset;
                ViewBag.MonthOffset = monthOffset;
                ViewBag.ViewMode = viewMode;
                ViewBag.MaGV = maGV;

                return View("ThoiKhoaBieuGiangVien", lich);
            }
            catch (Exception)
            {
                ViewBag.Error = "Đã xảy ra lỗi khi tải lịch giảng dạy.";
                return View(new List<LichGiangDayViewModel>());
            }
        }



        //Trang xem lớp phụ trách
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> LopPhuTrach()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Gọi API lấy danh sách lớp phụ trách (API đã sửa ở bước trước)
            var response = await client.GetAsync("api/laydanhsachlopphutrach");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể lấy danh sách lớp phụ trách.";
                return View(new List<LopPhuTrachViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(body);

            // Deserialize dữ liệu JSON thành List ViewModel
            var listLop = JsonConvert.DeserializeObject<List<LopPhuTrachViewModel>>(result.data.ToString());

            return View(listLop);
        }

        // 2. Xem chi tiết danh sách sinh viên của một lớp phụ trách
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> LopPhuTrachDetails(string maLop)
        {
            if (string.IsNullOrEmpty(maLop))
            {
                TempData["Error"] = "Thiếu mã lớp.";
                return RedirectToAction(nameof(LopPhuTrach));
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"api/danhsachsinhvien?maLop={maLop}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể lấy dữ liệu lớp học.";
                return RedirectToAction(nameof(LopPhuTrach));
            }

            var body = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(body);
            if (result == null || result.data == null)
            {
                TempData["Error"] = "API trả về dữ liệu rỗng.";
                return RedirectToAction(nameof(LopPhuTrach));
            }
            var data = result.data;

            var model = new ChiTietLopHocViewModel
            {
                MaLop = data.maLop,
                TenLop = data.tenLop,
                LopTruong = data.lopTruong,
                LopPho = data.lopPho,
                BiThu = data.biThu,
                TroLy = data.troLy,
                SinhVien = data.sinhVien != null
                    ? JsonConvert.DeserializeObject<List<dynamic>>(data.sinhVien.ToString())
                    : new List<dynamic>()
            };

            return View(model);
        }




        //Xem danh sách lớp học phần của giảng viên
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> LopHocPhan(int? selectedHocKyId)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            // Lấy access_token từ session
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/lophocphan");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể lấy danh sách lớp học phần từ API.";
                return View(new LopHocPhanFilterViewModel());
            }

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                ViewBag.Error = "Không tìm thấy dữ liệu lớp học phần.";
                return View(new LopHocPhanFilterViewModel());
            }

            var hocKyList = System.Text.Json.JsonSerializer.Deserialize<List<LopHocPhanByHocKyViewModel>>(
                dataElement.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<LopHocPhanByHocKyViewModel>();

            // Chọn học kỳ (nếu có param truyền vào, còn không lấy mới nhất)
            var latestHocKy = hocKyList.OrderByDescending(x => x.NgayBatDau).FirstOrDefault();
            int selectedId = selectedHocKyId ?? latestHocKy?.HocKyId ?? 0;
            var selectedLopHocPhans = hocKyList.FirstOrDefault(x => x.HocKyId == selectedId)?.LopHocPhans ?? new List<LopHocPhanViewModel>();

            // Tạo SelectList cho dropdown
            ViewBag.HocKyList = new SelectList(
                hocKyList.OrderByDescending(x => x.NgayBatDau),
                "HocKyId",     // Value
                "TenHocKy",    // Text
                selectedId     // Giá trị đang chọn
            );

            var model = new LopHocPhanFilterViewModel
            {
                DanhSachHocKy = hocKyList,
                SelectedHocKyId = selectedId,
                LopHocPhans = selectedLopHocPhans
            };

            return View(model);
        }

        // Trang chi tiết lớp học phần: Hiện danh sách sinh viên + buổi điểm danh
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> LopHocPhanDetails(string maLopHocPhan)
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 1️⃣ Lấy chi tiết lớp học phần
            var responseLHP = await client.GetAsync($"https://localhost:5001/api/chitietlophocphan/{maLopHocPhan}");
            if (!responseLHP.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không lấy được thông tin lớp học phần.";
                return View();
            }
            var lhpBody = await responseLHP.Content.ReadAsStringAsync();
            dynamic lhpResult = JsonConvert.DeserializeObject(lhpBody);
            var lopHocPhan = JsonConvert.DeserializeObject<LopHocPhanViewModel>(lhpResult.data.ToString());

            // 2️⃣ Lấy danh sách sinh viên
            var listSV = lopHocPhan.DanhSachSinhVien ?? new List<SinhVienViewModel>();

            // 3️⃣ Lấy danh sách buổi điểm danh
            var responseAttendance = await client.GetAsync($"https://localhost:5001/api/lophocphan/ma/{maLopHocPhan}/buoidiemdanh");
            var listAttendance = new List<AttendanceSessionViewModel>();
            if (responseAttendance.IsSuccessStatusCode)
            {
                var attBody = await responseAttendance.Content.ReadAsStringAsync();
                dynamic attResult = JsonConvert.DeserializeObject(attBody);
                listAttendance = JsonConvert.DeserializeObject<List<AttendanceSessionViewModel>>(attResult.data.ToString());
            }


            // 3️⃣ Lấy toàn bộ chi tiết điểm danh của các buổi này từ DB
            var buoiIds = listAttendance.Select(b => b.Id).ToList();
            var chiTietList = await _context.ChiTietDiemDanhs
                .Where(c => buoiIds.Contains(c.DiemDanhId))
                .ToListAsync();

            // 4️⃣ Lấy tất cả trạng thái để map
            var trangThaiDict = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "DiemDanh")
                .ToDictionaryAsync(t => t.Id, t => t.TenTrangThai);

            // 5️⃣ Mapping trạng thái thực tế cho từng sinh viên
            // 5️⃣ Mapping trạng thái thực tế cho từng sinh viên
            foreach (var buoi in listAttendance)
            {
                buoi.DanhSachSinhVien = new List<AttendanceDetailViewModel>();

                foreach (var sv in listSV)
                {
                    // Tìm bản ghi chi tiết điểm danh tương ứng
                    var chiTiet = chiTietList.FirstOrDefault(c => c.DiemDanhId == buoi.Id && c.SinhVienId == sv.Id);

                    // Bước 1: Đặt giá trị mặc định (Ví dụ: "Chưa điểm danh" hoặc "Vắng" tùy logic của bạn)
                    string tenTrangThaiHienThi = "Vắng mặt";

                    // Bước 2: Kiểm tra nếu có dữ liệu điểm danh trong DB
                    if (chiTiet != null)
                    {
                        // Bước 3: Tra cứu ID trong Dictionary để lấy tên
                        // Lưu ý: (int)chiTiet.TrangThaiId là vì có thể trong DB nó là nullable int?
                        if (trangThaiDict.ContainsKey((int)chiTiet.TrangThaiId))
                        {
                            tenTrangThaiHienThi = trangThaiDict[(int)chiTiet.TrangThaiId];
                        }
                        else
                        {
                            // Trường hợp có ID nhưng ID đó không nằm trong bảng Trạng Thái (lỗi dữ liệu hiếm gặp)
                            tenTrangThaiHienThi = "Trạng thái lỗi";
                        }
                    }

                    // Bước 4: Thêm vào danh sách hiển thị
                    buoi.DanhSachSinhVien.Add(new AttendanceDetailViewModel
                    {
                        SinhVienId = sv.Id,
                        TrangThai = tenTrangThaiHienThi, // Gán giá trị đã map được
                    });
                }
            }


            // 5️⃣ Truyền dữ liệu sang View
            ViewBag.LopHocPhan = lopHocPhan;
            ViewBag.SinhVienList = listSV;
            ViewBag.AttendanceList = listAttendance;
            ViewBag.MaLopHocPhan = maLopHocPhan;

            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttendanceSession(int id, string maLopHocPhan)
        {
            try
            {
                var diemDanh = await _context.DiemDanhs
                    .Include(dd => dd.ChiTietDiemDanhs)
                    .FirstOrDefaultAsync(dd => dd.Id == id);

                if (diemDanh == null)
                {
                    TempData["Error"] = "Không tìm thấy buổi điểm danh.";
                    return RedirectToAction("LopHocPhanDetails", new { maLopHocPhan });
                }

                // Xoá chi tiết điểm danh trước
                _context.ChiTietDiemDanhs.RemoveRange(diemDanh.ChiTietDiemDanhs);

                // Xoá buổi điểm danh
                _context.DiemDanhs.Remove(diemDanh);

                await _context.SaveChangesAsync();

                // 3. Xoá Firebase Realtime Database
                var firebaseClient = new FirebaseClient("https://bluenet-e6525-default-rtdb.firebaseio.com/");
                var path = $"attendancesessions/{id}";

                await firebaseClient.Child(path).DeleteAsync();


                TempData["Success"] = "Xóa buổi điểm danh thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xoá: {ex.Message}";
            }

            return RedirectToAction("LopHocPhanDetails", new { maLopHocPhan });
        }




        //Xem danh sách buổi điểm danh của lớp
        public async Task<IActionResult> AttendanceSessions(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"https://localhost:5001/api/lophocphan/{id}/buoidiemdanh");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không lấy được danh sách buổi điểm danh.";
                return View(new List<AttendanceSessionViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(body);

            var list = JsonConvert.DeserializeObject<List<AttendanceSessionViewModel>>(result.data.ToString());
            return View(list);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAttendanceSession(int LopHocPhanId, string maLopHocPhan)
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var now = DateTime.Now;
            var postData = new
            {
                Ngay = now,
                GhiChu = (string?)null,
                ExpireAt = now.AddSeconds(60)
            };

            var json = JsonConvert.SerializeObject(postData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"https://localhost:5001/api/lophocphan/{LopHocPhanId}/buoidiemdanh/tao", content);

            var body = await response.Content.ReadAsStringAsync();

            // Trong controller, khi POST tạo mới xong:
            if (response.IsSuccessStatusCode)
            {
                dynamic result = JsonConvert.DeserializeObject(body);
                int newSessionId = result.data.id;

                //TempData["Success"] = "Tạo buổi điểm danh thành công!";
                return RedirectToAction("AttendanceDetails", new { id = newSessionId });
            }
            else
            {
                // Lấy message chi tiết từ API trả về (body)
                string apiMessage = "";
                try
                {
                    var jobject = Newtonsoft.Json.Linq.JObject.Parse(body);
                    apiMessage = jobject?["message"]?.ToString() ?? "";
                }
                catch
                {
                    apiMessage = body;
                }

                TempData["Error"] = $"{apiMessage}";
                TempData["ErrorDetail"] = body; // Lưu toàn bộ body nếu cần log hoặc show chi tiết
                return RedirectToAction("LopHocPhanDetails", new { maLopHocPhan });
            }

        }


        public static string GenerateQrBase64(string text)
        {
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(qrData))
            using (var bitmap = qrCode.GetGraphic(10))
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return Convert.ToBase64String(stream.ToArray());
            }
        }


        // GET: Chi tiết buổi điểm danh
        public async Task<IActionResult> AttendanceDetails(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"https://localhost:5001/api/buoidiemdanh/{id}/chitiet");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không lấy được chi tiết buổi điểm danh.";
                return View();
            }

            var body = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(body);

            // Parse về ViewModel đầy đủ cho tất cả sinh viên
            var session = JsonConvert.DeserializeObject<AttendanceSessionDetailViewModel>(result.data.ToString());
            session.QrCodeBase64 = GenerateQrBase64(session.Code);
            ViewBag.MaLopHocPhan = session.MaLopHocPhan;

            // 2. Gọi API lấy trạng thái điểm danh
            var trangThaiRes = await client.GetAsync("https://localhost:5001/api/trangthai/loai/DiemDanh");
            if (trangThaiRes.IsSuccessStatusCode)
            {
                var trangThaiBody = await trangThaiRes.Content.ReadAsStringAsync();
                dynamic trangThaiResult = JsonConvert.DeserializeObject(trangThaiBody);
                // Tạo dictionary id => tên trạng thái
                var trangThaiDict = new Dictionary<int, string>();
                foreach (var item in trangThaiResult.data)
                {
                    int trangThaiId = (int)item.id;
                    string ten = (string)item.tenTrangThai;
                    trangThaiDict[trangThaiId] = ten;
                }

                ViewBag.TrangThaiDict = trangThaiDict;
            }
            else
            {
                ViewBag.TrangThaiDict = new Dictionary<int, string>(); // fallback rỗng
            }

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackToClassAndUpdateStatus(int buoiDiemDanhId, string maLopHocPhan)
        {
            await CapNhatTrangThaiBuoiDiemDanh(buoiDiemDanhId); // Hàm này bạn đã có
            return RedirectToAction("LopHocPhanDetails", new { maLopHocPhan });
        }


        private async Task CapNhatTrangThaiBuoiDiemDanh(int buoiDiemDanhId)
        {
            var buoi = await _context.DiemDanhs
                .Include(d => d.TrangThai)
                .FirstOrDefaultAsync(d => d.Id == buoiDiemDanhId);

            if (buoi == null) return;

            // Lấy trạng thái "Đã đóng"
            var trangThaiDong = await _context.TrangThais
                .FirstOrDefaultAsync(t => t.TenTrangThai == "Đã đóng" && t.LoaiTrangThai == "DiemDanh#");
            if (trangThaiDong != null)
            {
                buoi.TrangThaiId = trangThaiDong.Id;
                await _context.SaveChangesAsync();
            }
        }

        // POST: Đổi trạng thái điểm danh cho 1 sinh viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttendanceStatus(int diemDanhId, int sinhVienId, int trangThai)
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var postData = new
            {
                DiemDanhId = diemDanhId,
                SinhVienId = sinhVienId,
                TrangThai = trangThai
            };
            var json = JsonConvert.SerializeObject(postData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://localhost:5001/api/diemdanh/capnhattrangthai", content);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Đã cập nhật trạng thái!";
            else
                TempData["Error"] = "Lỗi cập nhật!";

            return RedirectToAction("AttendanceDetails", new { id = diemDanhId });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateAttendanceCode(int id)
        {
            // Gọi API để tạo lại code/mã điểm danh cho buổi này
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Gọi API tạo lại mã code mới, ví dụ POST hoặc PUT (tuỳ API bạn)
            var response = await client.PostAsync($"https://localhost:5001/api/buoidiemdanh/{id}/regeneratecode", null);


            var buoi = await _context.DiemDanhs
                            .Include(d => d.TrangThai)
                            .FirstOrDefaultAsync(d => d.Id == id);
            //Lấy Id của trạng thái đang diễn ra
            var trangThaiDienRa = await _context.TrangThais.FirstOrDefaultAsync(t => t.TenTrangThai == "Đang diễn ra" && t.LoaiTrangThai == "DiemDanh#");
            if (buoi != null && trangThaiDienRa != null)
            {
                buoi.TrangThaiId = trangThaiDienRa.Id;
                await _context.SaveChangesAsync();
            }
            if (response.IsSuccessStatusCode)
            {

                TempData["Success"] = "Đã tạo lại mã điểm danh mới!";
            }
            else
            {
                TempData["Error"] = "Tạo lại mã điểm danh thất bại!";
            }
            // Reload lại trang chi tiết buổi điểm danh
            return RedirectToAction("AttendanceDetails", new { id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiThongBaoLopHocPhan(string maLopHocPhan, [FromForm] GuiThongBaoLopRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Content))
            {
                TempData["Error"] = "Tiêu đề và nội dung không được để trống!";
                return RedirectToAction("LopHocPhanDetails", new { maLopHocPhan });
            }

            // Lấy access_token (nếu API cần xác thực)
            var token = HttpContext.Session.GetString("access_token");

            // Chuẩn bị HttpClient
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Gửi body dạng JSON
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"api/lophocphan/{maLopHocPhan}/guithongbao", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Đã gửi thông báo cho lớp học phần!";
            }
            else
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = "Gửi thông báo thất bại! " + errorMsg;
            }
            return RedirectToAction("LopHocPhanDetails", new { maLopHocPhan });
        }

        //View Xin vắng dạy
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> XinVangDay()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://localhost:5001/");

                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("api/lophocphan/hientai");

                List<LopHocPhanShortVM> danhSachLopHp = new();

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(body);

                    danhSachLopHp = JsonConvert.DeserializeObject<List<LopHocPhanShortVM>>(result.data.ToString());
                }

                ViewBag.LopHocPhanList = danhSachLopHp;

                return View(new XinVangDay());
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi: " + ex.Message;
                ViewBag.LopHocPhanList = new List<LopHocPhanShortVM>(); // tránh null
                return View(new XinVangDay());
            }

        }

        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XinVangDay(XinVangDay model)
        {
            if (!ModelState.IsValid)
            {
                // Lấy lại danh sách lớp học phần
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://localhost:5001/");
                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("api/lophocphan/hientai");
                List<LopHocPhanShortVM> danhSachLopHp = new();
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(body);
                    danhSachLopHp = JsonConvert.DeserializeObject<List<LopHocPhanShortVM>>(result.data.ToString());
                }
                ViewBag.LopHocPhanList = danhSachLopHp;

                return View(model);
            }

            try
            {
                var token = HttpContext.Session.GetString("access_token");
                if (string.IsNullOrEmpty(token))
                {
                    ViewBag.Error = "Không tìm thấy token trong session.";
                    return View(model);
                }

                // Decode JWT
                var payload = token.Split('.')[1];
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var jwtData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                // Lấy userId từ JWT (string)
                if (!jwtData.TryGetValue("userId", out var userIdStr))
                {
                    ViewBag.Error = "Token JWT không hợp lệ (không tìm thấy userId).";
                    return View(model);
                }

                // Nếu gv.UserId là string, so sánh trực tiếp
                var giangVien = await _context.GiangViens
                    .FirstOrDefaultAsync(gv => gv.UserId == userIdStr);

                if (giangVien == null)
                {
                    ViewBag.Error = "Giảng viên không tồn tại trong hệ thống.";
                    return View(model);
                }


                var lichHocId = model.LichHocId;
                var existingDon = await _context.XinVangDays
                    .Where(x => x.LichHocId == lichHocId && x.GiangVienId == giangVien.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingDon != null)
                {
                    var tenTrangThai = await _context.TrangThais
                        .Where(tt => tt.Id == existingDon.TrangThaiId)
                        .Select(tt => tt.TenTrangThai)
                        .FirstOrDefaultAsync();

                    if (tenTrangThai == "Chờ duyệt")
                    {
                        ViewBag.Error = "Đơn xin vắng dạy cho lịch học này đang chờ duyệt.";

                        return View(model);
                    }
                }

                //Lấy ngày từ LichHoc

                model.NgayXinVang = _context.LichHocs
                    .Where(lh => lh.Id == lichHocId)
                    .Select(lh => lh.Ngay)
                    .FirstOrDefault()
                    .ToString("dd/MM/yyyy");
                var ca = _context.LichHocs
                    .Where(lh => lh.Id == lichHocId)
                    .Select(lh => new { lh.GioBatDau, lh.GioKetThuc })
                    .FirstOrDefault();

                if (ca != null)
                {
                    model.CaXinVang = $"{ca.GioBatDau:hh\\:mm} - {ca.GioKetThuc:hh\\:mm}";
                }
                else
                {
                    model.CaXinVang = "";
                }


                // Gán GiangVienId trước khi lưu
                model.GiangVienId = giangVien.Id;
                model.CreatedAt = DateTime.Now;

                model.TrangThaiId = _context.TrangThais
                        .Where(tt => tt.TenTrangThai == "Chờ duyệt" && tt.LoaiTrangThai == "DonPhieu")
                        .Select(tt => tt.Id)
                        .FirstOrDefault();
                // Parse giờ từ string sang TimeSpan
                if (!string.IsNullOrEmpty(Request.Form["GioBatDauDayBu"]))
                    model.GioBatDauDayBu = TimeSpan.Parse(Request.Form["GioBatDauDayBu"]);
                if (!string.IsNullOrEmpty(Request.Form["GioKetThucDayBu"]))
                    model.GioKetThucDayBu = TimeSpan.Parse(Request.Form["GioKetThucDayBu"]);

                

                // Lưu vào DB
                _context.XinVangDays.Add(model);
                await _context.SaveChangesAsync();

                //TempData["Success"] = "Đơn xin vắng dạy đã được gửi!";
                return RedirectToAction("GuiDonThanhCong", new { id = model.Id });
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi: " + ex.Message;
                return View(model);
            }
        }


        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GuiDonThanhCong(int id)
        {
            var don = await _context.XinVangDays
                .Include(d => d.LopHocPhan)
                .Include(gv => gv.GiangVien)
                .Include(lh => lh.LichHoc)
                .Include(d => d.TrangThai)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (don == null) return NotFound();

            return View(don);
        }


        public async Task<IActionResult> DanhSachDonPhieu()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://localhost:5001/");
                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("api/danhsachdonphieu");
                List<XinVangDayViewModel> danhSachDon = new();

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(body);
                    danhSachDon = JsonConvert.DeserializeObject<List<XinVangDayViewModel>>(result.data.ToString());
                }

                return View(danhSachDon);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi: " + ex.Message;
                return View(new List<XinVangDayViewModel>());
            }
        }

        #region ======= QUẢN LÝ ĐIỂM (TEACHER) =======

        // Danh sách các lớp học phần mà GV đang dạy để vào chấm điểm
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ScoreManagerIndex(int? hocKyId)
        {
            // A. Lấy thông tin Giảng viên từ User đang đăng nhập
            var maGV = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var giangVien = await _context.GiangViens.FirstOrDefaultAsync(gv => gv.MaGiangVien == maGV);

            if (giangVien == null)
            {
                return RedirectToAction("Index"); // Hoặc trang báo lỗi
            }

            // B. Lấy danh sách lớp học phần do GV này phụ trách
            var query = _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.HocKy)
                .Include(l => l.BangDiems) // Include bảng điểm để hiển thị trạng thái
                .Where(l => l.GiangVienId == giangVien.Id) // CHỈ LẤY LỚP CỦA GV NÀY
                .AsQueryable();

            if (hocKyId.HasValue)
            {
                query = query.Where(l => l.HocKyId == hocKyId);
            }

            // C. Chuẩn bị ViewBags
            ViewBag.HocKyList = await _context.HocKys.OrderByDescending(h => h.NgayBatDau).ToListAsync();

            // Lấy ID trạng thái "Đã khóa" để View so sánh hiển thị icon khóa
            var lockedStatus = await _context.TrangThais
                .FirstOrDefaultAsync(t => t.LoaiTrangThai == "BangDiem" && t.TenTrangThai == "Đã khóa");
            ViewBag.LockedStatusId = lockedStatus?.Id ?? -1;

            var listLHP = await query.OrderByDescending(l => l.Id).ToListAsync();
            return View(listLHP);
        }
        [HttpGet]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Grading(int id) // id = LopHocPhanId
        {
            // A. Kiểm tra quyền truy cập (GV có dạy lớp này không?)
            var maGV = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var lhp = await _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.GiangVien)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lhp == null) return NotFound();

            if (lhp.GiangVien?.MaGiangVien != maGV)
            {
                return Forbid(); // Không phải lớp của mình -> Cấm
            }

            // B. Lấy trạng thái khóa
            var lockedStatus = await _context.TrangThais
                .FirstOrDefaultAsync(t => t.LoaiTrangThai == "BangDiem" && t.TenTrangThai == "Đã khóa");
            int lockedId = lockedStatus?.Id ?? -1;

            // C. Lấy dữ liệu sinh viên và điểm
            var enrollments = await _context.ChiTietLopHocPhans
                .Include(ct => ct.SinhVien)
                .Where(ct => ct.LopHocPhanId == id)
                .OrderBy(ct => ct.SinhVien.MSSV)
                .ToListAsync();

            var existingGrades = await _context.BangDiems
                .Where(bd => bd.LopHocPhanId == id)
                .ToListAsync();

            // D. Kiểm tra xem lớp này có đang bị khóa không?
            var firstGrade = existingGrades.FirstOrDefault();
            bool isLocked = firstGrade != null && firstGrade.TrangThaiId == lockedId;

            ViewBag.IsLocked = isLocked; // Truyền sang View

            // E. Map sang ViewModel
            var model = new ClassGradingVM
            {
                LopHocPhanId = lhp.Id,
                MaLopHocPhan = lhp.MaLopHocPhan,
                TenMonHoc = lhp.MonHoc?.TenMonHoc,
                TenGiangVien = lhp.GiangVien?.HoVaTenDem + " " + lhp.GiangVien?.Ten,
                Students = enrollments.Select(e =>
                {
                    var grade = existingGrades.FirstOrDefault(g => g.SinhVienId == e.SinhVienId);
                    return new StudentGradeRowVM
                    {
                        SinhVienId = (int)e.SinhVienId,
                        MSSV = e.SinhVien.MSSV,
                        HoTen = $"{e.SinhVien.HoVaTenDem} {e.SinhVien.Ten}",
                        BangDiemId = grade?.Id ?? 0,
                        DiemChuyenCan = grade?.DiemChuyenCan,
                        DiemCuoiKy = grade?.DiemCuoiKy
                    };
                }).ToList()
            };

            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGrades(ClassGradingVM model)
        {
            // A. Kiểm tra quyền sở hữu lớp
            var maGV = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var lhp = await _context.LopHocPhans.Include(l => l.GiangVien).FirstOrDefaultAsync(l => l.Id == model.LopHocPhanId);

            if (lhp == null || lhp.GiangVien?.MaGiangVien != maGV)
            {
                return Forbid();
            }

            // B. Kiểm tra trạng thái khóa (Quan trọng: Server-side check)
            var lockedStatus = await _context.TrangThais
                .FirstOrDefaultAsync(t => t.LoaiTrangThai == "BangDiem" && t.TenTrangThai == "Đã khóa");
            int lockedId = lockedStatus?.Id ?? -1;

            var checkGrade = await _context.BangDiems.FirstOrDefaultAsync(b => b.LopHocPhanId == model.LopHocPhanId);
            if (checkGrade != null && checkGrade.TrangThaiId == lockedId)
            {
                TempData["Error"] = "Bảng điểm đã bị KHÓA. Không thể cập nhật.";
                return RedirectToAction("Grading", new { id = model.LopHocPhanId });
            }

            // C. Tìm ID trạng thái "Cho phép sửa" để gán mặc định cho bản ghi mới
            var openStatus = await _context.TrangThais
                .FirstOrDefaultAsync(t => t.LoaiTrangThai == "BangDiem" && t.TenTrangThai == "Cho phép sửa");
            int openId = openStatus?.Id ?? 48;

            // D. Lưu dữ liệu
            foreach (var item in model.Students)
            {
                var bangDiem = await _context.BangDiems
                    .FirstOrDefaultAsync(bd => bd.LopHocPhanId == model.LopHocPhanId && bd.SinhVienId == item.SinhVienId);

                if (bangDiem == null)
                {
                    if (item.DiemChuyenCan.HasValue || item.DiemCuoiKy.HasValue)
                    {
                        bangDiem = new BangDiem
                        {
                            LopHocPhanId = model.LopHocPhanId,
                            SinhVienId = item.SinhVienId,
                            DiemChuyenCan = item.DiemChuyenCan,
                            DiemCuoiKy = item.DiemCuoiKy,
                            TrangThaiId = openId
                        };
                        _context.BangDiems.Add(bangDiem);
                    }
                }
                else
                {
                    bangDiem.DiemChuyenCan = item.DiemChuyenCan;
                    bangDiem.DiemCuoiKy = item.DiemCuoiKy;
                    _context.BangDiems.Update(bangDiem);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật bảng điểm thành công!";
            return RedirectToAction("Grading", new { id = model.LopHocPhanId });
        }

        #endregion


    }
}