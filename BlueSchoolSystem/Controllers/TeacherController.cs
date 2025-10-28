using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QRCoder;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static BlueSchoolSystem.APIControllers.APIGiangVienConTroller;

namespace BlueSchoolSystem.Controllers
{
    public class TeacherController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        public TeacherController(IHttpClientFactory httpClientFactory, ApplicationDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        //Trang chính
        public async Task<IActionResult> Index()
        {
            return View();
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

            // Gọi API chi tiết lớp học phần theo mã lớp
            var responseLHP = await client.GetAsync($"https://localhost:5001/api/chitietlophocphan/{maLopHocPhan}");
            if (!responseLHP.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không lấy được thông tin lớp học phần.";
                return View();
            }
            var lhpBody = await responseLHP.Content.ReadAsStringAsync();
            dynamic lhpResult = JsonConvert.DeserializeObject(lhpBody);
            var lopHocPhan = JsonConvert.DeserializeObject<LopHocPhanViewModel>(lhpResult.data.ToString());

            // Danh sách sinh viên
            var listSV = lopHocPhan.DanhSachSinhVien ?? new List<SinhVienViewModel>();

            // Lấy danh sách buổi điểm danh THEO MÃ lớp học phần
            var responseAttendance = await client.GetAsync($"https://localhost:5001/api/lophocphan/ma/{maLopHocPhan}/buoidiemdanh");
            var listAttendance = new List<AttendanceSessionViewModel>();
            if (responseAttendance.IsSuccessStatusCode)
            {
                var attBody = await responseAttendance.Content.ReadAsStringAsync();
                dynamic attResult = JsonConvert.DeserializeObject(attBody);
                listAttendance = JsonConvert.DeserializeObject<List<AttendanceSessionViewModel>>(attResult.data.ToString());
            }

            // Truyền sang View
            ViewBag.LopHocPhan = lopHocPhan;
            ViewBag.SinhVienList = listSV;
            ViewBag.AttendanceList = listAttendance;
            ViewBag.MaLopHocPhan = maLopHocPhan;


            return View();
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
        public async Task<IActionResult> CreateAttendanceSession(int LopHocPhanId)
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

            // Trong controller, khi POST tạo mới xong:
            if (response.IsSuccessStatusCode)
            {
                // Parse id của buổi điểm danh vừa tạo từ response
                var body = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(body);
                int newSessionId = result.data.id; // nhớ đúng key (id hoặc Id)

                TempData["Success"] = "Tạo buổi điểm danh thành công!";
                return RedirectToAction("AttendanceDetails", new { id = newSessionId });
            }
            else
            {
                TempData["Error"] = "Tạo buổi điểm danh thất bại!";
                return RedirectToAction("AttendanceSessions", new { id = LopHocPhanId });
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
                .FirstOrDefaultAsync(t => t.TenTrangThai == "Đã đóng" && t.LoaiTrangThai == "DiemDanh");
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

    }
}
