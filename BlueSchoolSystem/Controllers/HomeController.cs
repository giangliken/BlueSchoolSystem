using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BlueSchoolSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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
        public async Task<IActionResult> ThoiKhoaBieu() 
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token)) { 
                client.DefaultRequestHeaders.Authorization = new 
                    AuthenticationHeaderValue("Bearer", token); }

            // Lấy MSSV từ Claim (hoặc Session, tùy cách bạn lưu khi login)
            var mssv = User.Identity?.Name ;
            //Console.WriteLine("===== MSSV hiện tại: " + mssv);
            //ViewBag.MSSV = mssv; 
            //if (string.IsNullOrEmpty(mssv)) 
            //{ 
            //    ViewBag.Error = "Không xác định được MSSV của người dùng.";
            //    return View(new List<ThoiKhoaBieuViewModel>()); 
            //}
            
            var response = await client.GetAsync($"api/thoikhoabieusinhvien/{mssv}");
            if (!response.IsSuccessStatusCode) 
            { 
                ViewBag.Error = "Không thể lấy thời khóa biểu."; 
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
            var tkb = JsonSerializer.Deserialize<List<ThoiKhoaBieuViewModel>>(dataElement.ToString(), 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); 
            return View(tkb ?? new List<ThoiKhoaBieuViewModel>()); 
        }
        //Giao diện Lịch thi
        public IActionResult LichThi()
        {
            return View();
        }
        //Giao diện Xem điểm
        public IActionResult XemDiem()
        {
            return View();
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
