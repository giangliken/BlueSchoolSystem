using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BlueSchoolSystem.Controllers
{
    [Authorize(Roles = "Admin,Manager")]

    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ILogger<AdminController> logger, IHttpClientFactory httpClientFactory, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _userManager = userManager;
        }

        //Giao diện trang chủ của Admin
        public IActionResult Index()
        {
            var sinhVienCount = _context.SinhViens.Count();

            var khoaVienCount = _context.Khoas.Count();

            var giangVienCount = _context.GiangViens.Count();

            ViewBag.sinhVienCount = sinhVienCount;
            ViewBag.khoaVienCount = khoaVienCount;
            ViewBag.giangVienCount = giangVienCount;

            return View();
        }

        //Trang quản lý sinh viên
        public async Task<IActionResult> StudentManager(string? keyword, string? maLop, string? maKhoa, string? nienKhoa)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            // Lấy token
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Lấy danh sách lớp để đổ vào dropdown
            var classResponse = await client.GetAsync("api/laydanhsachlophoc");
            List<LopHocViewModel> lopList;
            if (classResponse.IsSuccessStatusCode)
            {
                var classJson = await classResponse.Content.ReadAsStringAsync();
                using var classDoc = JsonDocument.Parse(classJson);

                var classData = classDoc.RootElement.GetProperty("data");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                lopList = JsonSerializer.Deserialize<List<LopHocViewModel>>(classData.GetRawText(), options);
                ViewBag.LopList = lopList;
            }
            else
            {
                lopList = new List<LopHocViewModel>();
                ViewBag.LopList = lopList;
            }

            // Lấy danh sách khoa để đổ vào dropdown
            var facultyResponse = await client.GetAsync("api/laydanhsachkhoa");
            if (facultyResponse.IsSuccessStatusCode)
            {
                var facultyJson = await facultyResponse.Content.ReadAsStringAsync();
                using var facultyDoc = JsonDocument.Parse(facultyJson);

                var facultyData = facultyDoc.RootElement.GetProperty("data");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var khoaList = JsonSerializer.Deserialize<List<Khoa>>(facultyData.GetRawText(), options);
                ViewBag.KhoaList = khoaList;
            }
            else
            {
                ViewBag.KhoaList = new List<Khoa>();
            }

            //Lấy sách danh niên khóa để đổ vào dropdown
            var nienKhoaList = await _context.SinhViens
                .Select(sv => sv.NgayNhapHoc.Year)
                .Distinct()
                .OrderByDescending(year => year)
                .ToListAsync();

            ViewBag.ListNienKhoa = nienKhoaList;
            ViewBag.NienKhoa = nienKhoa;

            string malopQuery = maLop;

            if (string.IsNullOrEmpty(maLop) && !string.IsNullOrEmpty(maKhoa))
            {
                // Nếu chỉ chọn khoa, tự động fill malopQuery = tất cả mã lớp thuộc khoa đó
                malopQuery = string.Join(",", _context.LopHocs
                    .Where(l => l.Nganh != null && l.Nganh.Khoa != null && l.Nganh.Khoa.MaKhoa == maKhoa)
                    .Select(l => l.MaLop)
                    .ToList());
            }

            var url = "api/timkiemsinhvien?";


            if (!string.IsNullOrEmpty(keyword))
                url += "keyword=" + Uri.EscapeDataString(keyword) + "&";
            if (!string.IsNullOrEmpty(malopQuery))
                url += "maLop=" + Uri.EscapeDataString(malopQuery) + "&";
            if (!string.IsNullOrEmpty(maKhoa))
                url += "maKhoa=" + Uri.EscapeDataString(maKhoa) + "&";
            if (!string.IsNullOrEmpty(nienKhoa))
                url += "nienKhoa=" + Uri.EscapeDataString(nienKhoa) + "&";

            // In ra để debug thử URL thực sự gọi API:
            Console.WriteLine("API URL: " + url);



            HttpResponseMessage response;

            // Nếu không có filter nào thì lấy tất cả sinh viên
            if (string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(maLop) && string.IsNullOrEmpty(maKhoa) && string.IsNullOrEmpty(nienKhoa))
            {
                response = await client.GetAsync("api/laydanhsachsinhvien");
            }
            else
            {
                response = await client.GetAsync(url.TrimEnd('&'));
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
            ViewBag.Keyword = keyword;
            ViewBag.MaKhoa = maKhoa;
            ViewBag.MaLop = maLop;
            ViewBag.NienKhoa = nienKhoa;

            return View(students ?? new List<SinhVien>());
        }


        //Thêm sinh viên theo cách thủ công
        public async Task<IActionResult> AddStudent()
        {
            await LoadDropdownData();
            var model = new SinhVien
            {
                GioiTinh = true,
                NgayNhapHoc = DateTime.Today,
                User = new ApplicationUser()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(SinhVien model)
        {
            // Lấy ngành từ form
            var nganhHocId = Request.Form["NganhHocId"].ToString();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                // Đặt breakpoint hoặc log ra đây để biết trường nào lỗi
                return View(model);
            }


            // Map dữ liệu sang API request (CreateStudentWithUserRequest)
            var apiRequest = new CreateStudentWithUserRequest
            {
                UserName = model.MSSV,
                Email = model.User.Email,
                PhoneNumber = model.User.PhoneNumber,
                NganhHocId = nganhHocId,
                Password = "Abc@123", 
                Student = model 
            };

            var apiUrl = "https://localhost:5001/api/taomoisinhvien";

            var httpClient = new HttpClient();

            // Nếu API cần token thì thêm:
            var token = HttpContext.Session.GetString("access_token");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonConvert.SerializeObject(apiRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                // Thành công, redirect sang list
                TempData["Success"] = "Thêm sinh viên thành công!";
                return RedirectToAction("StudentManager");
            }
            else
            {
                // Lấy lỗi trả về từ API
                var apiError = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", "Có lỗi khi thêm sinh viên: " + apiError);
                await LoadDropdownData();
                return View(model);
            }
        }

        private async Task LoadDropdownData()
        {
            ViewBag.NganhList = await _context.NganhHocs.ToListAsync();
            ViewBag.TrangThaiList = new SelectList(
                await _context.TrangThais
                    .Where(tt => tt.LoaiTrangThai == "SinhVien")
                    .ToListAsync(),
                "Id",
                "TenTrangThai"
            );
            // Nếu cần dropdown lớp thì tùy logic filter ngành đã chọn
        }

        //Nhập danh sach sinh viên từ file Excel
        [HttpGet]
        public IActionResult ImportStudentListFromExcel()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportStudentListFromExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return View();
            }

            var students = new List<SinhVien>();

            using (var stream = new MemoryStream())
            {
                await excelFile.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;


                    var allLopHocs = _context.LopHocs
                        .Select(l => new { l.Id, l.MaLop })
                        .ToList()
                        .ToDictionary(x => x.MaLop.Trim().ToUpper(), x => x.Id);

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var maLop = worksheet.Cells[row, 12].Text.Trim().ToUpper();

                        int? lopId = null;
                        if (!string.IsNullOrEmpty(maLop) && allLopHocs.TryGetValue(maLop, out var foundId))
                            lopId = foundId;

                        if (lopId == null)
                        {
                            continue; 
                        }

                        var sv = new SinhVien
                        {


                            MSSV = worksheet.Cells[row, 1].Text.Trim(),
                            CCCD = worksheet.Cells[row, 2].Text.Trim(),
                            HoVaTenDem = worksheet.Cells[row, 3].Text.Trim(),
                            Ten = worksheet.Cells[row, 4].Text.Trim(),
                            NgaySinh = ParseExcelDate(worksheet.Cells[row, 5].Value),
                            GioiTinh = worksheet.Cells[row, 6].Text.Trim().ToUpper() == "NAM",
                            DiaChi = worksheet.Cells[row, 9].Text.Trim(),
                            NgayNhapHoc = ParseExcelDate(worksheet.Cells[row, 10].Value),
                            NgayTotNghiep = ParseExcelDate(worksheet.Cells[row, 11].Value),
                            TrangThaiId = 2,
                            LopId = lopId.Value,
                            User = new ApplicationUser
                            {
                                Email = worksheet.Cells[row, 8].Text.Trim(),
                                PhoneNumber = worksheet.Cells[row, 7].Text.Trim(),
                                UserName = worksheet.Cells[row, 1].Text.Trim()
                            }
                        };
                        students.Add(sv);
                    }
                }
            }

            // Gửi từng sinh viên qua API
            var apiUrl = "https://localhost:5001/api/taomoisinhvien";
            var httpClient = new HttpClient();
            var token = HttpContext.Session.GetString("access_token");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var errors = new List<string>();
            int successCount = 0;
            foreach (var sv in students)
            {
                var apiRequest = new CreateStudentWithUserRequest
                {
                    UserName = sv.MSSV,
                    Email = sv.User.Email,
                    PhoneNumber = sv.User.PhoneNumber,
                    Password = "Abc@123",
                    Student = sv
                };

                var json = JsonConvert.SerializeObject(apiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                    successCount++;
                else
                {
                    var apiError = await response.Content.ReadAsStringAsync();
                    errors.Add($"{sv.MSSV}: {apiError}");
                }
            }
            TempData["Success"] = $"Nhập thành công {successCount}/{students.Count} sinh viên!";
            if (errors.Count > 0)
                TempData["Error"] = "Lỗi import:<br>" + string.Join("<br>", errors);

            return RedirectToAction("StudentManager");
        }

        private DateTime ParseExcelDate(object val)
        {
            if (val == null) return DateTime.MinValue;
            if (val is double d)
                return DateTime.FromOADate(d);
            if (DateTime.TryParse(val.ToString(), out var dt))
                return dt;
            return DateTime.MinValue;
        }

        //File mẫu định dạng Import danh sách sinh viên
        public IActionResult DownloadStudentExcelTemplate()
        {
            // Tên các cột header đúng chuẩn UI
            var headers = new string[]
            {
                "Mã số sinh viên", "CCCD", "Họ và tên đệm", "Tên", "Ngày sinh", "Giới tính",
                "Số điện thoại", "Email", "Địa chỉ", "Ngày nhập học", "Ngày tốt nghiệp dự kiến",
                "Mã lớp", "Ghi chú"
            };

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("SinhVien_Template");

                // Ghi header
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());
                string fileName = "MauNhapDSSinhVien.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
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
                .Include(tt =>tt.TrangThai)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (sinhVien == null) return NotFound();

            ViewBag.TrangThaiList = _context.TrangThais
               .Where(t => t.LoaiTrangThai == "SinhVien")
               .Select(t => new { t.Id, t.TenTrangThai })
               .ToList();


            return View(sinhVien);
        }


        //Trang quản lí giảng viên
        public IActionResult TeacherManager()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id, string type)
        {
            ApplicationUser user = null;

            if (type == "SinhVien")
            {
                var sinhVien = await _context.SinhViens
                    .Include(sv => sv.User)
                    .FirstOrDefaultAsync(sv => sv.Id == id);
                user = sinhVien?.User;
            }
            else if (type == "GiangVien")
            {
                var giangVien = await _context.GiangViens
                    .Include(gv => gv.User)
                    .FirstOrDefaultAsync(gv => gv.Id == id);
                user = giangVien?.User;
            }

            if (user == null)
                return NotFound();

            var result = await ResetPasswordToDefaultAsync(user.Id);

            if (result)
                TempData["Success"] = "Đã reset lại mật khẩu thành công!";
            else
                TempData["Error"] = "Có lỗi xảy ra khi reset mật khẩu!";

            // Redirect về đúng trang chi tiết
            if (type == "SinhVien")
                return RedirectToAction("StudentDetails", new { id = id });
            else
                return RedirectToAction("TeacherDetails", "GiangVien", new { id = id });
        }

        //Hàm cấp lại mật khẩu tài khoản sinh viên
        public async Task<bool> ResetPasswordToDefaultAsync(string userId, string defaultPassword = "Abc@123")
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, defaultPassword);

            return result.Succeeded;
        }

        //Trang quản lí khoa viện
        public async Task<IActionResult> FacultyManager()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            HttpResponseMessage response;
            response = await client.GetAsync("api/laydanhsachkhoa");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể lấy danh sách khoa viện.";
                return View(new List<Khoa>());
            }

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                ViewBag.Error = "Không tìm thấy dữ liệu khoa viện.";
                return View(new List<Khoa>());
            }

            var khoas = JsonSerializer.Deserialize<List<Khoa>>(dataElement.ToString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

           

            return View(khoas ?? new List<Khoa>());

        }

        //Trang quản lí lớp học
        public async Task<IActionResult> ClassManager()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            HttpResponseMessage response;
            response = await client.GetAsync("api/laydanhsachlophoc");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể lấy danh sách lớp học.";
                return View(new List<LopHocViewModel>());
            }

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                ViewBag.Error = "Không tìm thấy dữ liệu lớp học.";
                return View(new List<LopHocViewModel>());
            }

            var lophocs = JsonSerializer.Deserialize<List<LopHocViewModel>>(dataElement.ToString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });



            return View(lophocs ?? new List<LopHocViewModel>());
        }

        //Chi tiết lớp học
        public async Task<IActionResult> ClassDetails(int? id)
        {
            if (id == null) return NotFound();

            var lopHoc = await _context.LopHocs
            .Include(lh => lh.Nganh)
                .ThenInclude(n => n.Khoa)
            .Include(lh => lh.ChiTietLopHocs)
                .ThenInclude(ct => ct.GiangVien)
                    .ThenInclude(gv => gv.User)
            .Include(lh => lh.ChiTietLopHocs)
                .ThenInclude(ct => ct.LopTruong)
                    .ThenInclude(sv => sv.User)
            .Include(lh => lh.ChiTietLopHocs)
                .ThenInclude(ct => ct.LopPho)
                    .ThenInclude(sv => sv.User)
            .Include(lh => lh.ChiTietLopHocs)
                .ThenInclude(ct => ct.BiThu)
                    .ThenInclude(sv => sv.User)

            .FirstOrDefaultAsync(lh => lh.Id == id);


            if (lopHoc == null) return NotFound();
            


            return View(lopHoc);
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
