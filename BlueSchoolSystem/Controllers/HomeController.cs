using BlueSchoolSystem.Models;
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
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> StudentManager()
        {

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/"); // sửa nếu API port khác

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync("api/student/laydanhsachsinhvien");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể lấy danh sách sinh viên.";
                return View(new List<Student>());
            }

            var body = await response.Content.ReadAsStringAsync();

            // Parse JSON thủ công để lấy "data"
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                ViewBag.Error = "Không tìm thấy dữ liệu sinh viên.";
                return View(new List<Student>());
            }

            var students = JsonSerializer.Deserialize<List<Student>>(dataElement.ToString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(students ?? new List<Student>());
        }



        public IActionResult Add()
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
