using BlueSchoolSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BlueSchoolSystem.Controllers
{
    [Authorize(Roles = "Admin,Manager")]

    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;

        public AdminController(ILogger<AdminController> logger, IHttpClientFactory httpClientFactory, ApplicationDbContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        //Giao diện trang chủ của Admin
        public IActionResult Index()
        {
            return View();
        }

        //Trang quản lý sinh viên
        public async Task<IActionResult> StudentManager(string? searchName, string? searchMSSV)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            HttpResponseMessage response;

            // Nếu có tìm kiếm thì gọi API lọc
            if (!string.IsNullOrEmpty(searchName) || !string.IsNullOrEmpty(searchMSSV))
            {
                // Gộp lại thành 1 từ khóa tìm kiếm
                var keyword = string.Join(" ", new[] { searchMSSV, searchName }
                    .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(keyword))
                    queryParams.Add($"keyword={Uri.EscapeDataString(keyword)}");

                var queryString = "?" + string.Join("&", queryParams);
                response = await client.GetAsync("api/timkiemsinhvien" + queryString);
            }
            else
            {
                // Ngược lại, gọi API lấy toàn bộ sinh viên
                response = await client.GetAsync("api/laydanhsachsinhvien");
            }

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể lấy danh sách sinh viên.";
                return View(new List<SinhVien>());
            }

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                ViewBag.Error = "Không tìm thấy dữ liệu sinh viên.";
                return View(new List<SinhVien>());
            }

            var students = JsonSerializer.Deserialize<List<SinhVien>>(dataElement.ToString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Truyền lại input
            ViewBag.SearchName = searchName;
            ViewBag.SearchMSSV = searchMSSV;

            return View(students ?? new List<SinhVien>());
        }

        //Thêm sinh viên theo cách thủ công
        public async Task<IActionResult> AddStudent()
        {
            return View();
        }


        //Xem thông tin chi tiết sinh viên
        public async Task<IActionResult> StudentDetails(int? id)
        {
            if (id == null) return NotFound();

            var sinhVien = await _context.SinhViens
                .Include(s => s.Lop)
                    .ThenInclude(l => l.Nganh)
                        .ThenInclude(n => n.Khoa)
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (sinhVien == null) return NotFound();

            return View(sinhVien);
        }

        public async Task<IActionResult> ActivityLogs()
        {
            var logs = await GetLogsFromApi();
            return View(logs);
        }

        public async Task<IActionResult> GetActivityLogsTable()
        {
            var logs = await GetLogsFromApi();
            return PartialView("_ActivityLogsTable", logs);
        }

        // Helper private để tái sử dụng
        private async Task<List<ActivityLog>> GetLogsFromApi()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync("api/xemnhatky");
            if (!response.IsSuccessStatusCode) return new List<ActivityLog>();

            var jsonString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            var resultArray = root.GetProperty("data").GetProperty("result");

            var logs = JsonSerializer.Deserialize<List<ActivityLog>>(resultArray, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return logs ?? new List<ActivityLog>();
        }



    }
}
