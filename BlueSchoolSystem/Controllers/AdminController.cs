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
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(searchName))
                    queryParams.Add($"keyword={searchName}");
                if (!string.IsNullOrEmpty(searchMSSV))
                    queryParams.Add($"mssv={searchMSSV}");

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


        public async Task<IActionResult> StudentDetails(int? id)
        {
            if (id == null) return NotFound();

            var sinhVien = await _context.SinhViens
                .Include(s => s.Lop)
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (sinhVien == null) return NotFound();

            return View(sinhVien);
        }


    }
}
