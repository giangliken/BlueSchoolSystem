using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using BlueSchoolSystem.Services;
using Humanizer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System.Diagnostics;
using System.Dynamic;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Collections.Generic;
using BlueSchoolSystem.Repository;

namespace BlueSchoolSystem.Controllers
{
    [Authorize(Roles = "Admin,Manager")]

    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration configuration;
        private readonly string? _apiBaseUrl;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityLogService _activityLogService;
        private readonly LopHocPhanService _lhpService;
        private readonly IEmailSender _emailSender;

        public AdminController(ILogger<AdminController> logger, IHttpClientFactory httpClientFactory, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration, IActivityLogService activityLogService, LopHocPhanService lhpService, IEmailSender emailSender)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _userManager = userManager;
            this.configuration = configuration;
            _lhpService = lhpService;

            _apiBaseUrl = configuration["ApiSettings:BaseUrl"];
            _activityLogService = activityLogService;

            _emailSender = emailSender;
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

            client.BaseAddress = new Uri(_apiBaseUrl);

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
            ViewBag.NganhList = await _context.NganhHocs.ToListAsync();
            ViewBag.TrangThaiList = new SelectList(
                await _context.TrangThais
                    .Where(tt => tt.LoaiTrangThai == "SinhVien")
                    .ToListAsync(),
                "Id",
                "TenTrangThai"
            );
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
                return View(model);
            }

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
                ViewBag.NganhList = await _context.NganhHocs.ToListAsync();
                ViewBag.TrangThaiList = new SelectList(
                    await _context.TrangThais
                        .Where(tt => tt.LoaiTrangThai == "SinhVien")
                        .ToListAsync(),
                    "Id",
                    "TenTrangThai"
                );
                return View(model);
            }
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
        public async Task<IActionResult> StudentDetails(string mssv)
        {
            if (string.IsNullOrEmpty(mssv))
            {
                TempData["Error"] = "Không tìm thấy sinh viên với MSSV này!";
                return RedirectToAction("StudentManager");
            }

            var sinhVien = await _context.SinhViens
                .Include(s => s.Lop)
                    .ThenInclude(l => l.Nganh)
                        .ThenInclude(n => n.Khoa)
                .Include(s => s.User)
                .Include(tt => tt.TrangThai)
                .FirstOrDefaultAsync(m => m.MSSV == mssv);

            if (sinhVien == null)
            {
                TempData["Error"] = $"Không tìm thấy sinh viên với MSSV: {mssv}";
                return RedirectToAction("StudentManager");
            }

            ViewBag.TrangThaiList = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "SinhVien")
                .ToListAsync();

            return View(sinhVien);
        }

        //Trang quản lí giảng viên
        public async Task<IActionResult> TeacherManager(string? keyword, string? maKhoa, int? trangThaiId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl);

                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Lấy list khoa để fill dropdown filter
                var facultyRes = await client.GetAsync("api/laydanhsachkhoa");
                if (facultyRes.IsSuccessStatusCode)
                {
                    var facultyJson = await facultyRes.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(facultyJson);
                    var data = doc.RootElement.GetProperty("data");
                    var khoaList = JsonConvert.DeserializeObject<List<Khoa>>(data.GetRawText());
                    ViewBag.KhoaList = khoaList;
                }
                else
                {
                    ViewBag.KhoaList = new List<Khoa>();
                }

                ViewBag.TrangThaiList = await _context.TrangThais
                    .Where(x => x.LoaiTrangThai == "GiangVien")
                    .ToListAsync();

                string url = "api/timkiemgiangvien?";
                if (!string.IsNullOrEmpty(keyword))
                    url += "keyword=" + Uri.EscapeDataString(keyword) + "&";
                if (!string.IsNullOrEmpty(maKhoa))
                    url += "maKhoa=" + Uri.EscapeDataString(maKhoa) + "&";
                if (trangThaiId.HasValue && trangThaiId > 0)
                    url += "trangThaiId=" + trangThaiId + "&";
                url = url.TrimEnd('&', '?');

                // Gọi API
                HttpResponseMessage response = await client.GetAsync(
                    (string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(maKhoa) && !trangThaiId.HasValue)
                        ? "api/laydanhsachgiangvien"
                        : url
                );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không lấy được danh sách giảng viên!";
                    return View(new List<GiangVien>());
                }

                var body = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                var dataJson = root.GetProperty("data").GetRawText();
                var teachers = JsonConvert.DeserializeObject<List<GiangVien>>(dataJson);

                // Truyền lại filter cho view
                ViewBag.Keyword = keyword;
                ViewBag.MaKhoa = maKhoa;
                ViewBag.TrangThaiId = trangThaiId;

                return View(teachers);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã có lỗi xảy ra: " + ex.Message;
                return View(new List<GiangVien>());
            }
        }


        //Thêm giảng viên mới
        public async Task<IActionResult> AddTeacher()
        {
            ViewBag.KhoaList = await _context.Khoas.ToListAsync();
            var trangThai = await _context.TrangThais
                .FirstOrDefaultAsync(tt => tt.TenTrangThai == "Đang công tác" && tt.LoaiTrangThai == "GiangVien");
            ViewBag.TrangThaiDangCongTacId = trangThai?.Id ?? 1;
            var model = new GiangVien
            {
                GioiTinh = true,
                User = new ApplicationUser()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(GiangVien model)
        {
            try
            {
                // Load lại các ViewBag cho View (kể cả khi có lỗi)
                ViewBag.KhoaList = await _context.Khoas.ToListAsync();
                var trangThai = await _context.TrangThais
                    .FirstOrDefaultAsync(tt => tt.TenTrangThai == "Đang công tác" && tt.LoaiTrangThai == "GiangVien");
                ViewBag.TrangThaiDangCongTacId = trangThai?.Id ?? 1;

                if (!ModelState.IsValid)
                    return View(model);


                var token = HttpContext.Session.GetString("access_token");
                // Nếu model.User có tồn tại (tức là từ form nhập), thì dùng lấy info xong set null

                string email = model.User?.Email;
                string phone = model.User?.PhoneNumber;
                model.User = null;

                var apiRequest = new CreateGiangVienWithUserRequest
                {
                    UserName = model.MaGiangVien,
                    Email = email,
                    PhoneNumber = phone,
                    Password = "Abc@123",
                    GiangVien = model
                };


                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl);
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonConvert.SerializeObject(apiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                //var jsonDebug = JsonConvert.SerializeObject(apiRequest, Formatting.Indented);

                var response = await client.PostAsync("/api/themgiangvien", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"Tạo giảng viên thành công! Mã CB/GV là: {model.MaGiangVien}";
                    return RedirectToAction("TeacherManager");
                }

                else
                {
                    // Parse lỗi trả về, nếu là JSON thì show lỗi đẹp hơn
                    var error = await response.Content.ReadAsStringAsync();
                    try
                    {
                        // Thử parse lỗi theo kiểu object nếu trả về JSON
                        dynamic errObj = JsonConvert.DeserializeObject(error);
                        if (errObj?.errors != null)
                        {
                            foreach (var field in errObj.errors)
                            {
                                foreach (var msg in field.Value)
                                {
                                    ModelState.AddModelError((string)field.Name, (string)msg);
                                }
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", $"API lỗi: {error}");
                        }
                    }
                    catch
                    {
                        // Nếu không parse được thì show lỗi thô
                        ModelState.AddModelError("", $"API lỗi: {error}");
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi không xác định: {ex.Message}");
                // Load lại ViewBag nếu có lỗi exception bất ngờ (mất mạng, timeout, ...)
                ViewBag.KhoaList = await _context.Khoas.ToListAsync();
                var trangThai = await _context.TrangThais
                    .FirstOrDefaultAsync(tt => tt.TenTrangThai == "Đang công tác" && tt.LoaiTrangThai == "GiangVien");
                ViewBag.TrangThaiDangCongTacId = trangThai?.Id ?? 1;

                return View(model);
            }
        }


        //Nhập danh sách giảng viên từ file excel
        [HttpGet]
        public IActionResult ImportTeacherListFromExcel()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportTeacherListFromExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel.";
                return View();
            }

            var teachers = new List<GiangVien>();
            var userInfos = new List<(string Email, string Phone)>();
            var importErrors = new List<string>();

            using (var stream = new MemoryStream())
            {
                await excelFile.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;


                    var allKhoa = _context.Khoas
                        .Select(k => new { k.Id, k.MaKhoa })
                        .ToList()
                        .ToDictionary(x => x.MaKhoa.Trim().ToUpper(), x => x.Id);

                    // Lấy trạng thái "Đang công tác"
                    var trangThai = await _context.TrangThais
                        .FirstOrDefaultAsync(tt => tt.TenTrangThai == "Đang công tác" && tt.LoaiTrangThai == "GiangVien");
                    int trangThaiDangCongTacId = trangThai?.Id ?? 1;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        string maGiangVien = worksheet.Cells[row, 1].Text.Trim();
                        string email = worksheet.Cells[row, 8].Text.Trim();
                        string phone = worksheet.Cells[row, 7].Text.Trim();
                        string maKhoa = worksheet.Cells[row, 10].Text.Trim().ToUpper();

                        // Check thiếu thông tin bắt buộc
                        if (string.IsNullOrEmpty(maGiangVien) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(maKhoa))
                        {
                            importErrors.Add($"Dòng {row}: Thiếu mã giảng viên, email hoặc mã khoa.");
                            continue;
                        }
                        // Check mã khoa hợp lệ
                        if (!allKhoa.TryGetValue(maKhoa, out int khoaId))
                        {
                            importErrors.Add($"Dòng {row}: Mã khoa \"{maKhoa}\" không hợp lệ.");
                            continue;
                        }
                        // Check mã giảng viên (username) đã tồn tại chưa (tùy nhu cầu, check DB)
                        bool exists = _context.Users.Any(u => u.UserName == maGiangVien);
                        if (exists)
                        {
                            importErrors.Add($"Dòng {row}: Mã giảng viên \"{maGiangVien}\" đã tồn tại.");
                            continue;
                        }

                        var gv = new GiangVien
                        {
                            MaGiangVien = maGiangVien,
                            CCCD = worksheet.Cells[row, 2].Text.Trim(),
                            HoVaTenDem = worksheet.Cells[row, 3].Text.Trim(),
                            Ten = worksheet.Cells[row, 4].Text.Trim(),
                            NgaySinh = ParseExcelDate(worksheet.Cells[row, 5].Value),
                            GioiTinh = worksheet.Cells[row, 6].Text.Trim().ToLower() == "nam",
                            DiaChi = worksheet.Cells[row, 9].Text.Trim(),
                            KhoaId = khoaId,
                            TrangThaiId = trangThaiDangCongTacId,
                            GhiChu = worksheet.Cells[row, 11].Text.Trim(),
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            // **KHÔNG set User!**
                        };
                        teachers.Add(gv);
                        userInfos.Add((email, phone));
                    }
                }
            }

            // Gửi từng giảng viên qua API
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var errors = new List<string>();
            int successCount = 0;
            for (int i = 0; i < teachers.Count; i++)
            {
                var gv = teachers[i];
                var (email, phone) = userInfos[i];

                var apiRequest = new CreateGiangVienWithUserRequest
                {
                    UserName = gv.MaGiangVien,
                    Email = email,
                    PhoneNumber = phone,
                    Password = "Abc@123",
                    GiangVien = gv
                };
                // Đảm bảo KHÔNG có User lồng bên trong:
                apiRequest.GiangVien.User = null;

                var json = JsonConvert.SerializeObject(apiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/themgiangvien", content);

                if (response.IsSuccessStatusCode)
                    successCount++;
                else
                {
                    var apiError = await response.Content.ReadAsStringAsync();
                    errors.Add($"{gv.MaGiangVien}: {apiError}");
                }
            }

            //if (importErrors.Count > 0)
            //{
            //    ViewBag.ImportErrors = importErrors;
            //    // Nếu muốn: truyền lại file mẫu để user download, hoặc danh sách lỗi
            //    return View("ImportTeacherListFromExcel"); // Tên view upload Excel
            //}

            string importResult = $"Nhập thành công {successCount}/{teachers.Count} giảng viên!";
            if (importErrors.Count > 0)
            {
                importResult += "<br/>Dữ liệu không hợp lệ:<br/>" + string.Join("<br/>", importErrors);
                TempData["Error"] = "Dữ liệu đầu vào không đúng";

                return View("ImportTeacherListFromExcel"); // Tên view upload Excel

            }
            if (errors.Count > 0)
                importResult += "<br/>Lỗi import:<br/>" + string.Join("<br/>", errors);

            TempData["Success"] = importResult;

            return RedirectToAction("TeacherManager");
        }

        //Mẫu excel nhập DS giảng viên
        public IActionResult DownloadTeacherExcelTemplate()
        {
            var headers = new string[]
            {
        "Mã giảng viên",      // 1
        "CCCD",               // 2
        "Họ và tên đệm",      // 3
        "Tên",                // 4
        "Ngày sinh",          // 5
        "Giới tính",          // 6 (Nam/Nữ)
        "Số điện thoại",      // 7
        "Email",              // 8
        "Địa chỉ",            // 9
        "Mã khoa",            // 10
        "Ghi chú"             // 11 mới
            };

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("GiangVien_Template");

                // Ghi header
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = headers[i];
                    ws.Cells[1, i + 1].Style.Font.Bold = true;
                }


                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());
                string fileName = "MauNhapDSGiangVien.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }


        // Xem thông tin chi tiết giảng viên
        public async Task<IActionResult> TeacherDetails(string maGiangVien)
        {
            if (string.IsNullOrEmpty(maGiangVien))
            {
                TempData["Error"] = "Không xác định được mã giảng viên.";
                return RedirectToAction("TeacherManager");
            }

            var giangVien = await _context.GiangViens
                .Include(gv => gv.Khoa)
                .Include(gv => gv.User)
                .Include(gv => gv.TrangThai)
                // include môn học qua bảng liên kết
                .Include(gv => gv.GiangVienMonHocs)
                    .ThenInclude(gvmh => gvmh.MonHoc)
                .FirstOrDefaultAsync(g => g.MaGiangVien == maGiangVien);

            if (giangVien == null)
            {
                TempData["Error"] = $"Không tìm thấy giảng viên với mã: {maGiangVien}";
                return RedirectToAction("TeacherManager");
            }

            // Lấy thêm danh sách môn học đã gán để view dễ render
            var assignedSubjects = giangVien.GiangVienMonHocs
                .Select(x => x.MonHoc)
                .ToList();

            ViewBag.AssignedSubjects = assignedSubjects;

            ViewBag.TrangThaiList = await _context.TrangThais
                .Where(x => x.LoaiTrangThai == "GiangVien")
                .ToListAsync();

            return View(giangVien);
        }





        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id, string type)
        {
            ApplicationUser user = null;
            string nameInfo = "";
            string codeInfo = "";

            if (type == "SinhVien")
            {
                var sinhVien = await _context.SinhViens
                    .Include(sv => sv.User)
                    .FirstOrDefaultAsync(sv => sv.Id == id);
                user = sinhVien?.User;
                if (sinhVien != null)
                {
                    codeInfo = sinhVien.MSSV;
                    nameInfo = $"{sinhVien.HoVaTenDem} {sinhVien.Ten}";
                }
            }
            else if (type == "GiangVien")
            {
                var giangVien = await _context.GiangViens
                    .Include(gv => gv.User)
                    .FirstOrDefaultAsync(gv => gv.Id == id);
                user = giangVien?.User;
                if (giangVien != null)
                {
                    codeInfo = giangVien.MaGiangVien;
                    nameInfo = $"{giangVien.HoVaTenDem} {giangVien.Ten}";
                }
            }

            if (user == null)
                return NotFound();

            var result = await ResetPasswordToDefaultAsync(user.Id);

            if (result)
                TempData["Success"] = $"Đã reset lại mật khẩu thành công cho {(type == "SinhVien" ? "sinh viên" : "giảng viên")}: Tài khoản: {codeInfo} - Họ tên: {nameInfo}";
            else
                TempData["Error"] = "Có lỗi xảy ra khi reset mật khẩu!";

            // Redirect về đúng trang chi tiết
            if (type == "SinhVien")
                return RedirectToAction("StudentDetails", new { mssv = codeInfo });
            else
                return RedirectToAction("TeacherDetails", new { maGiangVien = codeInfo });
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

        public async Task<IActionResult> FacultyManager(string maKhoa, string searchString)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            ViewBag.AccessToken = token;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {

                HttpResponseMessage response = await client.GetAsync("api/laydanhsachkhoa");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể lấy danh sách khoa viện (API Error).";
                    return View(new List<Khoa>());
                }

                var body = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                List<Khoa> listKhoaFull = new List<Khoa>();

                // Xử lý lấy dữ liệu từ JSON 
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataElement))
                {
                    listKhoaFull = JsonSerializer.Deserialize<List<Khoa>>(dataElement.ToString(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    try
                    {
                        listKhoaFull = JsonSerializer.Deserialize<List<Khoa>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch { }
                }

                if (listKhoaFull == null) listKhoaFull = new List<Khoa>();

                ViewBag.ListKhoa = new SelectList(listKhoaFull, "MaKhoa", "TenKhoa", maKhoa);

                // 2. Bắt đầu lọc dữ liệu (Filter)
                var query = listKhoaFull.AsQueryable();

                if (!string.IsNullOrEmpty(maKhoa))
                {
                    query = query.Where(k => k.MaKhoa == maKhoa);
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.Trim().ToLower();
                    query = query.Where(k => k.MaKhoa.ToLower().Contains(searchString)
                                          || k.TenKhoa.ToLower().Contains(searchString));
                    ViewData["CurrentFilter"] = searchString;
                }
                return View(query.ToList());
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi kết nối: " + ex.Message;
                return View(new List<Khoa>());
            }
        }
        //Chi tiết khoa viện
        public async Task<IActionResult> FacultyDetails(int id)
        {
            var khoa = await _context.Khoas
                .Include(k => k.ChiTietKhoaViens)
                    .ThenInclude(ct => ct.TruongKhoa).ThenInclude(u => u.User)
                .Include(k => k.ChiTietKhoaViens)
                    .ThenInclude(ct => ct.PhoKhoa).ThenInclude(u => u.User)
                .Include(k => k.ChiTietKhoaViens)
                    .ThenInclude(ct => ct.TroLiKhoa).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (khoa == null)
                return NotFound();

            return View(khoa);
        }

        private async Task<(SelectList giangVienList, int? truong, int? pho, int? troLy)>
            CreateFacultySelectLists(int facultyId)
        {
            var khoa = await _context.Khoas
                .Include(k => k.ChiTietKhoaViens)
                .FirstOrDefaultAsync(k => k.Id == facultyId);

            var detail = khoa?.ChiTietKhoaViens?.FirstOrDefault();

            int? currentTruongId = detail?.TruongKhoaId;
            int? currentPhoId = detail?.PhoKhoaId;
            int? currentTroLyId = detail?.TroLiKhoaId;

            var giangViens = await _context.GiangViens
                .Where(gv => gv.KhoaId == facultyId) // Giảng viên phải thuộc khoa
                .Select(gv => new
                {
                    gv.Id,
                    HoTen = gv.HoVaTenDem + " " + gv.Ten + " (" + gv.MaGiangVien + ")"
                })
                .OrderBy(gv => gv.HoTen)
                .ToListAsync();

            var giangVienList = new SelectList(giangViens, "Id", "HoTen", currentTruongId);

            return (giangVienList, currentTruongId, currentPhoId, currentTroLyId);
        }

        public async Task<IActionResult> EditFaculty(int id)
        {
            var khoa = await _context.Khoas
                        .Include(k => k.ChiTietKhoaViens)
                            .ThenInclude(ct => ct.TruongKhoa)
                        .Include(k => k.ChiTietKhoaViens)
                            .ThenInclude(ct => ct.PhoKhoa)
                        .Include(k => k.ChiTietKhoaViens)
                            .ThenInclude(ct => ct.TroLiKhoa)
                        .FirstOrDefaultAsync(k => k.Id == id);

            if (khoa == null)
            {
                TempData["Error"] = "Không tìm thấy khoa cần chỉnh sửa!";
                return RedirectToAction("FacultyManager");
            }

            var lists = await LoadFacultySelectLists(id);

            ViewBag.TruongKhoaList = lists.TruongList;
            ViewBag.PhoKhoaList = lists.PhoList;
            ViewBag.TroLyList = lists.TroLyList;

            return View(khoa);
        }
        private async Task<(SelectList TruongList, SelectList PhoList, SelectList TroLyList)>
    LoadFacultySelectLists(int facultyId, int? forcedTruong = null, int? forcedPho = null, int? forcedTroLy = null)
        {
            var khoa = await _context.Khoas
                        .Include(k => k.ChiTietKhoaViens)
                        .FirstOrDefaultAsync(k => k.Id == facultyId);

            var detail = khoa?.ChiTietKhoaViens?.FirstOrDefault();

            int? selTruong = forcedTruong ?? detail?.TruongKhoaId;
            int? selPho = forcedPho ?? detail?.PhoKhoaId;
            int? selTroLy = forcedTroLy ?? detail?.TroLiKhoaId;
            var giangViens = await _context.GiangViens
                                .Where(gv => gv.KhoaId == facultyId)
                                .Select(gv => new
                                {
                                    Id = gv.Id,
                                    HoTen = gv.HoVaTenDem + " " + gv.Ten + " (" + gv.MaGiangVien + ")"
                                })
                                .OrderBy(gv => gv.HoTen)
                                .ToListAsync();

            var truongList = new SelectList(giangViens, "Id", "HoTen", selTruong);
            var phoList = new SelectList(giangViens, "Id", "HoTen", selPho);
            var troLyList = new SelectList(giangViens, "Id", "HoTen", selTroLy);

            return (truongList, phoList, troLyList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFaculty(Khoa model, int? TruongKhoaId, int? PhoKhoaId, int? TroLiKhoaId)
        {
            var selected = new List<int>();
            if (TruongKhoaId.HasValue && TruongKhoaId.Value > 0) selected.Add(TruongKhoaId.Value);
            if (PhoKhoaId.HasValue && PhoKhoaId.Value > 0) selected.Add(PhoKhoaId.Value);
            if (TroLiKhoaId.HasValue && TroLiKhoaId.Value > 0) selected.Add(TroLiKhoaId.Value);

            if (selected.Count != selected.Distinct().Count())
            {
                ModelState.AddModelError("", "Một giảng viên không thể giữ nhiều hơn một chức vụ trong khoa.");
                TempData["Error"] = "Một giảng viên không thể giữ nhiều hơn một chức vụ.";
            }

            if (!ModelState.IsValid)
            {
                // Tạo lại select lists và truyền selected values (sau khi user đã chọn)
                var lists = await LoadFacultySelectLists(model.Id, TruongKhoaId, PhoKhoaId, TroLiKhoaId);
                ViewBag.TruongKhoaList = lists.TruongList;
                ViewBag.PhoKhoaList = lists.PhoList;
                ViewBag.TroLyList = lists.TroLyList;

                return View(model);
            }

            try
            {
                var khoa = await _context.Khoas
                            .Include(k => k.ChiTietKhoaViens)
                            .FirstOrDefaultAsync(k => k.Id == model.Id);

                if (khoa == null) return RedirectToAction("FacultyManager");

                // Cập nhật cơ bản
                khoa.MaKhoa = model.MaKhoa;
                khoa.TenKhoa = model.TenKhoa;

                var detail = khoa.ChiTietKhoaViens.FirstOrDefault();
                if (detail == null)
                {
                    detail = new ChiTietKhoaVien { KhoaId = khoa.Id };
                    _context.ChiTietKhoaViens.Add(detail);
                }

                detail.TruongKhoaId = TruongKhoaId > 0 ? TruongKhoaId : null;
                detail.PhoKhoaId = PhoKhoaId > 0 ? PhoKhoaId : null;
                detail.TroLiKhoaId = TroLiKhoaId > 0 ? TroLiKhoaId : null;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật khoa thành công!";
                return RedirectToAction("FacultyDetails", new { id = model.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật khoa ID {Id}", model.Id);
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);

                var lists = await LoadFacultySelectLists(model.Id, TruongKhoaId, PhoKhoaId, TroLiKhoaId);
                ViewBag.TruongKhoaList = lists.TruongList;
                ViewBag.PhoKhoaList = lists.PhoList;
                ViewBag.TroLyList = lists.TroLyList;

                return View(model);
            }
        }


        //Trang quản lí lớp học
        public async Task<IActionResult> ClassManager(string? maKhoa, string? maNganh, string? keyword, string? khoaHoc)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var url = "api/laydanhsachlophoctheodieukien?";
            if (!string.IsNullOrEmpty(maKhoa)) url += $"maKhoa={maKhoa}&";
            if (!string.IsNullOrEmpty(maNganh)) url += $"maNganh={maNganh}&";
            if (!string.IsNullOrEmpty(keyword)) url += $"keyword={keyword}&";
            if (!string.IsNullOrEmpty(khoaHoc)) url += $"khoaHoc={khoaHoc}&";
            url = url.TrimEnd('&', '?');

            var response = await client.GetAsync(string.IsNullOrEmpty(url) ? "api/laydanhsachlophoctheodieukien" : url);
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

            var classes = JsonSerializer.Deserialize<List<LopHocViewModel>>(dataElement.ToString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Dropdown filters
            ViewBag.KhoaList = await _context.Khoas.ToListAsync();
            if (!string.IsNullOrEmpty(maKhoa))
            {
                ViewBag.NganhList = await _context.NganhHocs
                    .Where(n => n.Khoa.MaKhoa == maKhoa)
                    .ToListAsync();
            }
            else
            {
                ViewBag.NganhList = await _context.NganhHocs.ToListAsync();
            }
            ViewBag.KhoaHocList = (await _context.LopHocs
                .Select(l => l.MaLop.Substring(0, 2))
                .Distinct()
                .ToListAsync())
                .Select(x => "20" + x) // "22" → "2022"
                .OrderByDescending(x => x)
                .ToList();


            // Gửi lại filters cho View giữ trạng thái
            ViewBag.MaKhoa = maKhoa;
            ViewBag.MaNganh = maNganh;
            ViewBag.Keyword = keyword;
            ViewBag.KhoaHoc = khoaHoc;

            return View(classes ?? new List<LopHocViewModel>());
        }


        //Thêm lớp học
        [HttpGet]
        public async Task<IActionResult> AddClass()
        {
            // Load danh sách ngành để đổ dropdown
            ViewBag.NganhList = await _context.NganhHocs.ToListAsync();
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> AddClass(CreateClassRequest request)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PostAsJsonAsync("api/themlophoc", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Thêm lớp thành công!";
                return RedirectToAction("ClassManager");
            }

            // Lấy nội dung JSON lỗi trả về
            var body = await response.Content.ReadAsStringAsync();

            // Tách lỗi message nếu API có property 'message'
            try
            {
                var json = JsonDocument.Parse(body);
                if (json.RootElement.TryGetProperty("message", out var msg))
                {
                    ModelState.AddModelError("", msg.GetString());
                }
                else
                {
                    ModelState.AddModelError("", "Thêm lớp thất bại!");
                }
            }
            catch
            {
                ModelState.AddModelError("", "Lỗi không xác định từ API");
            }

            // Reload dropdown ngành
            ViewBag.NganhList = await _context.NganhHocs.ToListAsync();

            return View(request);
        }


        //Chi tiết lớp học
        public async Task<IActionResult> ClassDetails(string maLop)
        {
            if (string.IsNullOrEmpty(maLop)) return NotFound();

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
            .FirstOrDefaultAsync(lh => lh.MaLop == maLop);

            if (lopHoc == null) return NotFound();

            return View(lopHoc);
        }


        // Xóa lớp học
        [HttpPost]
        public async Task<IActionResult> DeleteClass(string maLop)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://localhost:5001/");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.DeleteAsync($"api/xoalophoc?maLop={maLop}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Xóa lớp học thành công!";
                return RedirectToAction("ClassManager");
            }

            var body = await response.Content.ReadAsStringAsync();

            string message = "Không thể xóa lớp học!";

            try
            {
                using var json = JsonDocument.Parse(body);

                if (json.RootElement.TryGetProperty("message", out var msgProperty))
                {
                    message = msgProperty.GetString() ?? message;
                }
            }
            catch
            {
                // fallback nếu parse lỗi
            }

            TempData["Error"] = message;
            return RedirectToAction("ClassDetails", new { maLop });


        }

        // Phương thức private helper để tạo tất cả SelectLists cần thiết
        private async Task<(SelectList nganhList, SelectList giangVienList, SelectList lopTruongList, SelectList lopPhoList, SelectList biThuList)>
            CreateClassSelectLists(int classId, int? currentLopTruongId, int? currentLopPhoId, int? currentBiThuId)
        {
            var lopHocForFilters = await _context.LopHocs
                .Include(lh => lh.ChiTietLopHocs)
                .FirstOrDefaultAsync(lh => lh.Id == classId);


            var chiTiet = lopHocForFilters?.ChiTietLopHocs.FirstOrDefault();
            int? initialGiangVienId = chiTiet?.GiangVienId;


            int? selectedLopTruongId = currentLopTruongId ?? chiTiet?.LopTruongId;
            int? selectedLopPhoId = currentLopPhoId ?? chiTiet?.LopPhoId;
            int? selectedBiThuId = currentBiThuId ?? chiTiet?.BiThuId;


            var nganhList = new SelectList(
                await _context.NganhHocs.ToListAsync(),
                "Id", "TenNganh", lopHocForFilters?.NganhId
            );


            var sinhViens = await _context.SinhViens
                .Where(sv => sv.LopId == classId)
                .Select(sv => new { Id = sv.Id, HoTen = sv.HoVaTenDem + " " + sv.Ten + " (" + sv.MSSV + ")" })
                .OrderBy(sv => sv.HoTen)
                .ToListAsync();


            var giangViens = await _context.GiangViens
                .GroupJoin(_context.ChiTietLopHocs, gv => gv.Id, ct => ct.GiangVienId, (gv, ctGroup) => new
                {
                    GiangVien = gv,
                    SoLopDamNhan = ctGroup.Count()
                })
                .Select(result => new
                {
                    Id = result.GiangVien.Id,
                    HoTen = result.GiangVien.HoVaTenDem + " " + result.GiangVien.Ten + " (" + result.GiangVien.MaGiangVien + ")",
                    SoLopDamNhan = result.SoLopDamNhan
                })
                .Where(gv => gv.SoLopDamNhan < 5 || gv.Id == initialGiangVienId) // Luôn cho phép GV hiện tại
                .OrderBy(gv => gv.HoTen)
                .ToListAsync();


            var giangVienList = new SelectList(giangViens, "Id", "HoTen", initialGiangVienId);
            var lopTruongList = new SelectList(sinhViens, "Id", "HoTen", selectedLopTruongId);
            var lopPhoList = new SelectList(sinhViens, "Id", "HoTen", selectedLopPhoId);
            var biThuList = new SelectList(sinhViens, "Id", "HoTen", selectedBiThuId);

            return (nganhList, giangVienList, lopTruongList, lopPhoList, biThuList);
        }
        // 1. GET: Hiển thị form sửa lớp học
        public async Task<IActionResult> EditClass(string maLop)
        {
            if (string.IsNullOrEmpty(maLop))
            {
                TempData["Error"] = "Không tìm thấy mã lớp học!";
                return RedirectToAction("ClassManager");
            }

            var lopHoc = await _context.LopHocs
                .Include(lh => lh.Nganh)
                .Include(lh => lh.ChiTietLopHocs)
                .FirstOrDefaultAsync(lh => lh.MaLop == maLop);

            if (lopHoc == null)
            {
                TempData["Error"] = "Không tìm thấy lớp học cần chỉnh sửa!";
                return RedirectToAction("ClassManager");
            }


            var lists = await CreateClassSelectLists(lopHoc.Id, null, null, null);

            ViewBag.NganhList = lists.nganhList;
            ViewBag.GiangVienList = lists.giangVienList;
            ViewBag.LopTruongList = lists.lopTruongList;
            ViewBag.LopPhoList = lists.lopPhoList;
            ViewBag.BiThuList = lists.biThuList;

            return View(lopHoc);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClass(LopHoc model, int? LopTruongId, int? LopPhoId, int? BiThuId, int? GiangVienId)
        {
            var selectedStudentIds = new List<int>();
            if (LopTruongId.HasValue && LopTruongId.Value > 0)
                selectedStudentIds.Add(LopTruongId.Value);
            if (LopPhoId.HasValue && LopPhoId.Value > 0)
                selectedStudentIds.Add(LopPhoId.Value);
            if (BiThuId.HasValue && BiThuId.Value > 0)
                selectedStudentIds.Add(BiThuId.Value);


            if (!ModelState.IsValid || selectedStudentIds.Count() != selectedStudentIds.Distinct().Count())
            {
                if (selectedStudentIds.Count() != selectedStudentIds.Distinct().Count())
                {
                    ModelState.AddModelError("", "Lỗi: Một sinh viên không thể giữ nhiều hơn một chức vụ cán sự (Lớp trưởng, Lớp phó, Bí thư). Vui lòng chọn lại.");
                    TempData["Error"] = "Lỗi: Một sinh viên không thể giữ nhiều hơn một chức vụ cán sự.";
                }


                var lists = await CreateClassSelectLists(model.Id, LopTruongId, LopPhoId, BiThuId);

                ViewBag.NganhList = lists.nganhList;
                ViewBag.GiangVienList = lists.giangVienList;
                ViewBag.LopTruongList = lists.lopTruongList;
                ViewBag.LopPhoList = lists.lopPhoList;
                ViewBag.BiThuList = lists.biThuList;

                return View(model);
            }


            try
            {
                var existingLopHoc = await _context.LopHocs
                    .Include(lh => lh.ChiTietLopHocs)
                    .FirstOrDefaultAsync(lh => lh.Id == model.Id);

                if (existingLopHoc == null)
                {
                    TempData["Error"] = "Không tìm thấy lớp học trong cơ sở dữ liệu.";
                    return RedirectToAction("ClassManager");
                }


                existingLopHoc.MaLop = model.MaLop;
                existingLopHoc.TenLop = model.TenLop;
                existingLopHoc.NganhId = model.NganhId;

                var chiTiet = existingLopHoc.ChiTietLopHocs.FirstOrDefault();
                if (chiTiet == null)
                {
                    chiTiet = new ChiTietLopHoc { LopHocId = existingLopHoc.Id };
                    _context.ChiTietLopHocs.Add(chiTiet);
                }

                chiTiet.LopTruongId = LopTruongId > 0 ? LopTruongId : null;
                chiTiet.LopPhoId = LopPhoId > 0 ? LopPhoId : null;
                chiTiet.BiThuId = BiThuId > 0 ? BiThuId : null;
                chiTiet.GiangVienId = GiangVienId > 0 ? GiangVienId : null;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật lớp học thành công!";
                return RedirectToAction("ClassDetails", new { maLop = model.MaLop });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật lớp học ID {Id}", model.Id);
                ModelState.AddModelError("", $"Lỗi không xác định: {ex.Message}");


                var lists = await CreateClassSelectLists(model.Id, LopTruongId, LopPhoId, BiThuId);
                ViewBag.NganhList = lists.nganhList;
                ViewBag.GiangVienList = lists.giangVienList;
                ViewBag.LopTruongList = lists.lopTruongList;
                ViewBag.LopPhoList = lists.lopPhoList;
                ViewBag.BiThuList = lists.biThuList;

                return View(model);
            }
        }


        // GET: Trang tạo lớp tự động
        [HttpGet]
        public async Task<IActionResult> AutoCreateClass()
        {
            ViewBag.NganhList = await _context.NganhHocs
                .Include(n => n.Khoa)
                .ToListAsync();

            return View();
        }



        // POST: Tạo lớp tự động
        [HttpPost]
        public async Task<IActionResult> AutoCreateClass(AutoClassRequest req)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.NganhList = await _context.NganhHocs.Include(n => n.Khoa).ToListAsync();
                return View(req);
            }

            // Kiểm tra số lượng hợp lệ
            if (req.SoLuong < 1)
            {
                ModelState.AddModelError("", "Số lượng lớp phải >= 1");
                ViewBag.NganhList = await _context.NganhHocs.Include(n => n.Khoa).ToListAsync();
                return View(req);
            }

            // Lấy ngành
            var nganh = await _context.NganhHocs
                .Include(n => n.Khoa)
                .FirstOrDefaultAsync(n => n.Id == req.NganhId);

            if (nganh == null)
            {
                TempData["Error"] = "Ngành không tồn tại!";
                return RedirectToAction("AutoCreateClass");
            }

            string maKhoa = nganh.Khoa.MaKhoa; // DTH, DHQ, QTK...
            string khoaShort = req.Khoa.ToString().Substring(2, 2); // 2022 → 22

            // Lấy danh sách mã lớp đã có để tránh trùng
            var existing = await _context.LopHocs
                .Where(l => l.MaLop.StartsWith(khoaShort + maKhoa))
                .Select(l => l.MaLop)
                .ToListAsync();

            // Bộ chữ cái
            var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            var created = new List<string>();
            int index = 1;

            while (created.Count < req.SoLuong)
            {
                // A1 A2 B1 B2...
                int letterIndex = (index - 1) / 2;
                int number = ((index - 1) % 2) + 1;
                char letter = letters[letterIndex];

                string maLop = $"{khoaShort}{maKhoa}{letter}{number}";

                if (!existing.Contains(maLop))
                {
                    created.Add(maLop);

                    _context.LopHocs.Add(new LopHoc
                    {
                        MaLop = maLop,
                        TenLop = maLop,
                        NganhId = req.NganhId
                    });
                }

                index++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo {created.Count} lớp mới!";
            return RedirectToAction("ClassManager");
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
        //quản lý học kì
        // lấy danh sách học kì
        // GET: /Admin/SemesterManager
        public async Task<IActionResult> SemesterManager(string searchName, int? year)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var trangThaiList = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "HocKy")
                .ToListAsync();
            ViewBag.TrangThaiList = trangThaiList;

            HttpResponseMessage response;
            try { response = await client.GetAsync("api/hocky"); }
            catch
            {
                TempData["Error"] = "Không thể kết nối đến API.";
                return View(new List<HocKy>());
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể lấy dữ liệu học kỳ từ API.";
                return View(new List<HocKy>());
            }

            var body = await response.Content.ReadAsStringAsync();
            var hockys = JsonConvert.DeserializeObject<List<HocKy>>(JObject.Parse(body)["data"].ToString());

            // Lọc tên và năm
            if (!string.IsNullOrEmpty(searchName))
                hockys = hockys.Where(h => h.TenHocKy.Contains(searchName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (year.HasValue)
                hockys = hockys.Where(h => h.NgayBatDau.Year == year.Value).ToList();

            ViewBag.SearchName = searchName;
            ViewBag.Year = year;
            hockys = hockys
                .OrderByDescending(h => h.NgayBatDau)
                .ThenByDescending(h => h.NgayKetThuc)
                .ToList();
            return View(hockys);
        }

        // POST: /Admin/DeleteSemester
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSemester(int id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"api/hocky/{id}");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Xóa học kỳ thành công!";
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized ||
                     response.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này.";
            }
            else
            {
                try
                {
                    var errorObj = JObject.Parse(content);
                    TempData["Error"] = (string)errorObj["message"] ?? "Không thể xóa học kỳ.";
                }
                catch
                {
                    TempData["Error"] = "Không thể xóa học kỳ.";
                }
            }

            return RedirectToAction(nameof(SemesterManager));
        }

        // GET: /Admin/CreateSemester
        public async Task<IActionResult> CreateSemester()
        {
            // Cần phải có DTO để chứa các field: TenHocKy, NgayBatDau, NgayKetThuc, NgayCongBoTKB
            return View(new HocKyDTO { NgayBatDau = DateTime.Today.AddDays(1), NgayKetThuc = DateTime.Today.AddMonths(4), NgayCongBoTKB = DateTime.Today.AddMonths(1) });
        }

        // POST: /Admin/CreateSemester
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSemester(HocKyDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            // Logic kiểm tra ngày tháng
            if (dto.NgayBatDau >= dto.NgayKetThuc)
            {
                ModelState.AddModelError(nameof(dto.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");
                return View(dto);
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("access_token");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                // Chuẩn bị request body
                var json = JsonConvert.SerializeObject(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gọi API tạo học kỳ
                var response = await client.PostAsync("api/hocky", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"Tạo Học kỳ **{dto.TenHocKy}** thành công.";
                    return RedirectToAction(nameof(SemesterManager));
                }
                else
                {
                    var apiError = await response.Content.ReadAsStringAsync();
                    // Cố gắng parse lỗi từ API nếu có
                    try
                    {
                        dynamic errObj = JsonConvert.DeserializeObject(apiError);
                        ModelState.AddModelError("", errObj?.message ?? "Lỗi tạo học kỳ từ API.");
                    }
                    catch
                    {
                        ModelState.AddModelError("", "Lỗi không xác định khi tạo học kỳ.");
                    }
                    return View(dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối khi tạo học kỳ.");
                ModelState.AddModelError("", $"Lỗi kết nối: {ex.Message}");
                return View(dto);
            }
        }
        // Phương thức này thường được gọi bằng Ajax/Fetch từ trang SemesterManager
        [HttpPost]
        public async Task<IActionResult> UpdateSemesterStatus(int hockyId, int trangThaiId)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("access_token");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Tạo DTO tương ứng với API yêu cầu
            var dto = new UpdateTrangThaiDTO { HocKyId = hockyId, TrangThaiId = trangThaiId };
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PutAsync("api/hocky/trangthai", content);

                if (response.IsSuccessStatusCode)
                {
                    // Parse response để lấy message thành công
                    var body = await response.Content.ReadAsStringAsync();
                    dynamic successObj = JsonConvert.DeserializeObject(body);
                    return Json(new { success = true, message = successObj.message });
                }
                else
                {
                    var apiError = await response.Content.ReadAsStringAsync();
                    dynamic errObj = JsonConvert.DeserializeObject(apiError);
                    return Json(new { success = false, message = errObj?.message ?? "Cập nhật trạng thái không thành công." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối khi cập nhật trạng thái học kỳ.");
                return Json(new { success = false, message = $"Lỗi kết nối: {ex.Message}" });
            }
        }
        // Cập nhật thông tin Học kỳ
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("hocky/{id}")]
        public async Task<IActionResult> UpdateHocKy(int id, [FromBody] HocKyDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { result = false, message = "Dữ liệu không hợp lệ." });

            var hocky = await _context.HocKys.FindAsync(id);
            if (hocky == null)
                return NotFound(new { result = false, message = "Không tìm thấy Học kỳ." });

            // Kiểm tra ngày tháng
            if (dto.NgayBatDau >= dto.NgayKetThuc)
                return BadRequest(new { result = false, message = "Ngày bắt đầu phải trước ngày kết thúc." });

            string oldTenHocKy = hocky.TenHocKy;
            DateTime oldNgayBatDau = hocky.NgayBatDau;
            DateTime oldNgayKetThuc = hocky.NgayKetThuc;

            // Cập nhật thông tin
            hocky.TenHocKy = dto.TenHocKy;
            hocky.NgayBatDau = dto.NgayBatDau;
            hocky.NgayKetThuc = dto.NgayKetThuc;

            try
            {
                _context.HocKys.Update(hocky);
                await _context.SaveChangesAsync();

                // Log hoạt động
                var userId = User.FindFirst("userId")?.Value;
                var userName = User.FindFirst("username")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await _activityLogService.LogAsync(
                        userId: userId,
                        userName: userName ?? "Unknown",
                        device: Request.Headers["User-Agent"].ToString(),
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                        actionType: "UPDATE",
                        tableName: "HocKys",
                        objectId: hocky.Id.ToString(),
                        description: $"Cập nhật Học kỳ {oldTenHocKy} ({oldNgayBatDau:dd/MM/yyyy} - {oldNgayKetThuc:dd/MM/yyyy}) thành {hocky.TenHocKy} ({hocky.NgayBatDau:dd/MM/yyyy} - {hocky.NgayKetThuc:dd/MM/yyyy})"
                    );
                }

                return Ok(new { result = true, code = 200, message = $"Học kỳ {hocky.TenHocKy} đã được cập nhật thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật Học kỳ.");
                return StatusCode(500, new { result = false, message = "Lỗi server khi cập nhật Học kỳ." });
            }
        }

        //Quản lý đợt đăng ký
        #region ======= Quản lý đợt đăng ký (MỚI: Chọn LHP) =======




        /// GET: /Admin/RegistrationPeriods - Trang chọn LHP để mở đăng ký
        /// </summary>
        public async Task<IActionResult> RegistrationPeriods(int hocKyId = 0)
        {
            // B1: Lấy danh sách Học kỳ (để dropdown chọn)
            var hockys = await GetFilteredHocKyListFromDb();
            ViewBag.HocKyList = new SelectList(hockys, "Id", "TenHocKy", hocKyId);
            ViewBag.SelectedHocKyId = hocKyId;

            var lhpList = new List<ExpandoObject>();

            if (hocKyId > 0)
            {
                // B2: Lấy danh sách LHP đang Mở và còn chỗ (Gọi API)
                var (success, data, error) = await CallApiAsync("api/lhp-for-reg", HttpMethod.Get);

                if (success)
                {
                    lhpList = JsonConvert.DeserializeObject<List<ExpandoObject>>(
                         data.GetProperty("data").GetRawText(),
                         new JsonSerializerSettings { Converters = { new ExpandoObjectConverter() } }
                    );

                    // Lọc LHP theo Học kỳ đã chọn
                    lhpList = lhpList
                        .Where(l =>
                        {
                            var dict = (IDictionary<string, object>)l;
                            if (dict.ContainsKey("hocKyId"))
                            {
                                int hocKyIdFromLhp = Convert.ToInt32(dict["hocKyId"]);
                                return hocKyIdFromLhp == hocKyId;
                            }
                            return false;
                        })
                        .ToList();
                }

                // LẤY NGÀY BẮT ĐẦU VÀ KẾT THÚC CỦA HỌC KỲ
                var (hkSuccess, hkNgayBatDau, hkNgayKetThuc, hkError) = await _lhpService.GetHocKyDatesAsync(hocKyId);

                DateTime proposedStart;
                DateTime proposedEnd;

                if (hkSuccess)
                {
                    // ĐẶT GIỜ CỤ THỂ VÀ TÍNH NGÀY ĐỀ XUẤT
                    proposedStart = hkNgayBatDau.Date.AddDays(14).AddHours(10); // Bắt đầu 10:00 sáng, 2 tuần sau
                    proposedEnd = hkNgayKetThuc.Date.AddHours(23).AddMinutes(59); // Kết thúc 23:59 đêm
                }
                else
                {
                    // Dùng ngày mặc định nếu thất bại (dù đã có cơ chế kiểm tra trong service)
                    proposedStart = DateTime.Today.AddDays(1).AddHours(10);
                    proposedEnd = DateTime.Today.AddDays(7).AddHours(23).AddMinutes(59);
                }

                // CHUYỂN DateTime SANG CHUỖI ISO CHUẨN TRONG CONTROLLER
                ViewBag.DefaultNgayBatDau = proposedStart.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DefaultNgayKetThuc = proposedEnd.ToString("yyyy-MM-ddTHH:mm");
            }
            else
            {
                // Khi chưa chọn Học kỳ, gán chuỗi rỗng để JavaScript không lỗi
                ViewBag.DefaultNgayBatDau = "";
                ViewBag.DefaultNgayKetThuc = "";
            }

            ViewBag.LhpList = lhpList;

            return View(new QuanLyDangKyLHPRequestDTO { HocKyId = hocKyId });
        }

        /// <summary>
        /// POST: Xử lý tạo đợt đăng ký cho các LHP đã chọn
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSelectedRegistrationPeriods(QuanLyDangKyLHPRequestDTO dto)
        {
            // B1: Tải lại ViewBag nếu ModelState không hợp lệ (Dùng lại logic của GET)
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng điền đủ thông tin bắt buộc.";
                return RedirectToAction(nameof(RegistrationPeriods), new { hocKyId = dto.HocKyId });
            }

            if (dto.LopHocPhanIds == null || !dto.LopHocPhanIds.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một Lớp Học Phần để mở đăng ký.";
                return RedirectToAction(nameof(RegistrationPeriods), new { hocKyId = dto.HocKyId });
            }

            // B2: Gửi yêu cầu tạo đợt đăng ký cho các LHP đã chọn (Gọi API mới)
            var (success, data, error) = await CallApiAsync("api/dotdangky-lhp", HttpMethod.Post, dto);

            if (success)
            {
                string message = data.GetProperty("message").GetString() ?? "Mở đăng ký cho các LHP đã chọn thành công.";
                TempData["Success"] = message;
            }
            else
            {
                string message = error;
                try
                {
                    // Cố gắng parse message lỗi từ API
                    if (error.Contains("API error:") && error.Contains("{"))
                    {
                        var jsonError = error.Substring(error.IndexOf('{'));
                        dynamic errObj = JsonConvert.DeserializeObject(jsonError);
                        message = errObj?.message ?? error;
                    }
                }
                catch { }
                TempData["Error"] = $"Lỗi: {message}";
            }

            return RedirectToAction(nameof(CourseClassManager));
        }
        #endregion
        // Quản lý lớp học phần
        #region ======= Helper Methods =======

        private async Task PopulateViewBags(LopHocPhanCreateDTO dto = null)
        {
            // Lấy ID của các trạng thái hợp lệ cho việc mở lớp học phần
            var validStatusNames = new List<string> { "Mới tạo", "Đang diễn ra" };

            // Tìm ID của các trạng thái này trong bảng TrangThai
            var validStatusIds = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "HocKy" && validStatusNames.Contains(t.TenTrangThai))
                .Select(t => t.Id)
                .ToListAsync();

            // Lọc Học kỳ theo các trạng thái hợp lệ
            var hocKys = await _context.HocKys
                .Where(hk => validStatusIds.Contains(hk.TrangThaiId)) // Giả sử HocKy có TrangThaiId
                .ToListAsync();

            var monHocs = await _context.MonHocs.ToListAsync();
            var phongHocs = await _context.PhongHocs.ToListAsync();

            ViewBag.HocKyList = new SelectList(hocKys, "Id", "TenHocKy", dto?.HocKyId);
            ViewBag.MonHocList = new SelectList(monHocs, "Id", "TenMonHoc", dto?.MonHocId);
            ViewBag.PhongHocList = new SelectList(phongHocs, "Id", "MaPhongHoc", dto?.PhongHocId);


            if (dto?.MonHocId > 0)
            {
                var giangViens = await _context.GiangViens
    // Lọc giảng viên: kiểm tra xem có bất kỳ bản ghi nào trong GiangVienMonHocs
    // mà MonHocId khớp với MonHocId đã chọn hay không.
    .Where(gv => gv.GiangVienMonHocs.Any(gvmh => gvmh.MonHocId == dto.MonHocId))
    .Select(gv => new { gv.Id, HoTen = gv.HoVaTenDem + " " + gv.Ten })
    .ToListAsync();
                ViewBag.GiangVienList = new SelectList(giangViens, "Id", "HoTen", dto.GiangVienId);
            }
            else
            {
                ViewBag.GiangVienList = new SelectList(new List<dynamic>(), "Id", "HoTen");
            }

            ViewBag.TrangThaiLHPList = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "LopHocPhan")
                .ToListAsync();
        }
        #endregion
        #region ======= Helper  =======
        private async Task<(bool success, JsonElement data, string error)> CallApiAsync(string url, HttpMethod method, object body = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl); // Dùng Base URL đã được inject

                // Lấy token từ session và thiết lập Authorization Header
                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                HttpResponseMessage response;
                HttpContent contentToSend = null;

                if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "DELETE"))
                {
                    var json = JsonConvert.SerializeObject(body);
                    contentToSend = new StringContent(json, Encoding.UTF8, "application/json");
                }

                // Xử lý request
                if (method == HttpMethod.Get)
                {
                    response = await client.GetAsync(url);
                }
                else if (method == HttpMethod.Post)
                {
                    response = await client.PostAsync(url, contentToSend);
                }
                else if (method == HttpMethod.Put)
                {
                    response = await client.PutAsync(url, contentToSend);
                }
                else if (method == HttpMethod.Delete)
                {
                    // DELETE thường không cần body, nhưng tôi dùng SendAsync để xử lý tốt hơn
                    var request = new HttpRequestMessage(HttpMethod.Delete, url);
                    if (contentToSend != null) request.Content = contentToSend;
                    response = await client.SendAsync(request);
                }
                else
                {
                    throw new NotImplementedException("HTTP method chưa hỗ trợ.");
                }

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Cố gắng phân tích lỗi từ API nếu có
                    return (false, default, $"API error: {response.StatusCode}, {content}");
                }

                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(content);
                }
                catch // Xử lý trường hợp API trả về 200 OK nhưng body rỗng hoặc không phải JSON (ví dụ: 204 No Content)
                {
                    // Trả về JsonElement rỗng
                    return (true, JsonDocument.Parse("{}").RootElement, null);
                }

                return (true, doc.RootElement, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gọi API");
                return (false, default, ex.Message);
            }
        }
        private IActionResult RePopulateViewAndReturn(object dto, LopHocPhan lhp = null)
        {
            ViewBag.LHP = lhp;

            // Tải lại các ViewBag cần thiết. Lưu ý: PhongHocList, Tiet Maps được tải lại
            // ngay trước khi gọi RePopulateViewAndReturn trong các POST actions.

            // Đảm bảo DaysOfWeek luôn có mặt
            ViewBag.DaysOfWeek = new List<object>
    {
        new { Id = 2, Name = "Thứ Hai" }, new { Id = 3, Name = "Thứ Ba" }, new { Id = 4, Name = "Thứ Tư" },
        new { Id = 5, Name = "Thứ Năm" }, new { Id = 6, Name = "Thứ Sáu" }, new { Id = 7, Name = "Thứ Bảy" },
        new { Id = 8, Name = "Chủ Nhật" }
    };

            if (lhp != null && lhp.GiangVien != null)
                ViewBag.GiangVien = $"{lhp.GiangVien.HoVaTenDem} {lhp.GiangVien.Ten} (Mã: {lhp.GiangVien.MaGiangVien})";
            else
                ViewBag.GiangVien = "Không rõ";

            // Phải đảm bảo ViewBag.PhongHocList, ViewBag.TietStartMap, ViewBag.TietEndMap 
            // đã được gán TRƯỚC khi gọi hàm này trong các [HttpPost].

            return View(dto);
        }

        #endregion

        #region ======= Course Class Manager =======

        public async Task<IActionResult> CourseClassManager()
        {
            // API call giữ nguyên
            var (success, data, error) = await CallApiAsync("api/lophocphans", HttpMethod.Get);
            if (!success)
            {
                TempData["Error"] = error ?? "Không thể lấy danh sách Lớp Học Phần.";
                return View(new List<object>());
            }

            var lhpList = JsonConvert.DeserializeObject<List<ExpandoObject>>(
                data.GetProperty("data").GetRawText(),
                new JsonSerializerSettings { Converters = { new ExpandoObjectConverter() } }
            );

            // Dùng Service để load ViewBags
            var lists = await _lhpService.PopulateCourseClassViewBagsAsync();
            ViewBag.HocKyList = lists.hocKyList;
            ViewBag.MonHocList = lists.monHocList;
            ViewBag.PhongHocList = lists.phongHocList;
            ViewBag.GiangVienList = lists.giangVienList;
            ViewBag.TrangThaiLHPList = lists.trangThaiList;

            return View(lhpList);
        }

        public async Task<IActionResult> CourseClassDetail(int id)
        {
            // Dùng Service để lấy chi tiết LHP
            var lhp = await _lhpService.GetLopHocPhanDetailsAsync(id);

            if (lhp == null)
            {
                TempData["Error"] = "Không tìm thấy lớp học phần.";
                return RedirectToAction(nameof(CourseClassManager));
            }

            // Dùng Service để lấy lịch học
            ViewBag.LichHoc = await _lhpService.GetLichHocByLHPIdAsync(id);

            return View(lhp);
        }

        public async Task<IActionResult> StudentInCourseClass(int lhpId)
        {
            // 1. Lấy thông tin LHP để hiển thị tiêu đề
            var lhp = await _lhpService.GetLopHocPhanDetailsAsync(lhpId);
            if (lhp == null)
            {
                TempData["Error"] = "Lớp học phần không tồn tại.";
                return RedirectToAction(nameof(CourseClassManager));
            }

            // 2. Lấy danh sách sinh viên từ Service
            var students = await _lhpService.GetStudentsInClassAsync(lhpId);

            // 3. Truyền dữ liệu sang View
            ViewBag.LHP = lhp;
            ViewBag.TotalStudents = students.Count;

            return View(students);
        }

        #endregion

        #region ======= Create Course Class =======

        public async Task<IActionResult> CreateCourseClass()
        {
            // Dùng Service để load ViewBags
            var lists = await _lhpService.PopulateCourseClassViewBagsAsync();
            ViewBag.HocKyList = lists.hocKyList;
            ViewBag.MonHocList = lists.monHocList;
            ViewBag.PhongHocList = lists.phongHocList;
            ViewBag.GiangVienList = lists.giangVienList;
            ViewBag.TrangThaiLHPList = lists.trangThaiList;

            var dto = new LopHocPhanCreateDTO
            {
                NgayBatDauLHP = DateTime.Today,
                NgayKetThucLHP = DateTime.Today.AddMonths(3)
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseClass(LopHocPhanCreateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                // Dùng Service để load lại ViewBags
                var lists = await _lhpService.PopulateCourseClassViewBagsAsync(dto);
                ViewBag.HocKyList = lists.hocKyList;
                ViewBag.MonHocList = lists.monHocList;
                ViewBag.PhongHocList = lists.phongHocList;
                ViewBag.GiangVienList = lists.giangVienList;
                return View(dto);
            }

            // Dùng Service để tự sinh mã LHP
            if (string.IsNullOrWhiteSpace(dto.MaLopHocPhan))
                dto.MaLopHocPhan = await _lhpService.GenerateAutoMaLHPAsync(dto.HocKyId, dto.MonHocId);

            // API call giữ nguyên
            var (success, data, error) = await CallApiAsync("api/lophocphan", HttpMethod.Post, dto);

            if (success)
            {
                int newLhpId = data.GetProperty("lopHocPhanId").GetInt32();
                TempData["Success"] = data.GetProperty("message").GetString() ?? $"Tạo Lớp Học Phần **{dto.MaLopHocPhan}** thành công!";
                return RedirectToAction(nameof(CourseClassDetail), new { id = newLhpId });
            }

            ModelState.AddModelError("", error ?? "Lỗi tạo Lớp Học Phần từ API.");
            // Dùng Service để load lại ViewBags
            var errorLists = await _lhpService.PopulateCourseClassViewBagsAsync(dto);
            ViewBag.HocKyList = errorLists.hocKyList;
            ViewBag.MonHocList = errorLists.monHocList;
            ViewBag.PhongHocList = errorLists.phongHocList;
            ViewBag.GiangVienList = errorLists.giangVienList;
            return View(dto);
        }

        #endregion

        #region ======= Add Schedule (Thủ công) =======

        public async Task<IActionResult> AddSchedule(int lhpId)
        {
            var lhp = await _lhpService.GetLopHocPhanDetailsAsync(lhpId);

            if (lhp == null)
            {
                TempData["Error"] = "Không tìm thấy Lớp Học Phần.";
                return RedirectToAction(nameof(CourseClassManager));
            }

            // 1. Gán ViewBag cần thiết
            ViewBag.LHP = lhp;
            // Dùng Service để lọc phòng học
            var phongHocs = await _lhpService.GetPhongHocListAsync(lhp.MonHocId);
            ViewBag.PhongHocList = new SelectList(phongHocs, "Id", "MaPhongHoc");

            // Dùng Service để lấy Map Tiết học
            ViewBag.TietStartMap = _lhpService.GetTietStartMap();
            ViewBag.TietEndMap = _lhpService.GetTietEndMap();

            // Gán thông tin giảng viên
            if (lhp.GiangVien != null)
                ViewBag.GiangVien = $"{lhp.GiangVien.HoVaTenDem} {lhp.GiangVien.Ten} (Mã: {lhp.GiangVien.MaGiangVien})";
            else
                ViewBag.GiangVien = "Không rõ";

            // 2. Tạo DTO và Return View
            var model = new AddScheduleDTO
            {
                LopHocPhanId = lhp.Id,
                Ngay = DateTime.Now.Date,
                TietBatDau = 2,
                TietKetThuc = 6
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(AddScheduleDTO dto)
        {
            var lhp = await _lhpService.GetLopHocPhanDetailsAsync(dto.LopHocPhanId);

            // GÁN MAP TIẾT HỌC CHO TRƯỜNG HỢP LỖI
            ViewBag.TietStartMap = _lhpService.GetTietStartMap();
            ViewBag.TietEndMap = _lhpService.GetTietEndMap();

            // Dùng Service để lấy danh sách phòng học hợp lệ (để tạo SelectList khi return View)
            var validPhongHocs = await _lhpService.GetPhongHocListAsync(lhp?.MonHocId);
            ViewBag.PhongHocList = new SelectList(validPhongHocs, "Id", "MaPhongHoc");
            ViewBag.LHP = lhp; // Cần thiết cho RePopulateViewAndReturn

            // 1. Validation cơ bản
            if (!ModelState.IsValid || lhp == null || dto.PhongHocId <= 0)
            {
                ModelState.AddModelError("", "Vui lòng điền đủ thông tin bắt buộc.");
                return View(dto);
            }

            // 2. Validation Tiết học
            if (dto.TietBatDau > dto.TietKetThuc)
            {
                ModelState.AddModelError(nameof(dto.TietKetThuc), "Tiết kết thúc phải lớn hơn hoặc bằng Tiết bắt đầu.");
                return View(dto);
            }

            // 3. Validation Phía Server: Kiểm tra Phòng đã chọn có hợp lệ không
            if (!validPhongHocs.Any(ph => ph.Id == dto.PhongHocId))
            {
                ModelState.AddModelError(nameof(dto.PhongHocId), "Phòng học được chọn không phù hợp với loại môn học (Thực hành/Lý thuyết).");
                return View(dto);
            }

            // 4. CHUYỂN ĐỔI TIẾT SANG TIMESPAN (Dùng Service)
            var (startTime, endTime) = _lhpService.ConvertTietToTimeSpan(dto.TietBatDau, dto.TietKetThuc);

            // 5. Chuẩn bị đối tượng LichHocDTO
            var scheduleToCreate = new LichHocDTO
            {
                LopHocPhanId = dto.LopHocPhanId,
                Ngay = dto.Ngay,
                GioBatDau = startTime,
                GioKetThuc = endTime,
                PhongHocId = dto.PhongHocId
            };

            // 6. KIỂM TRA TRÙNG LỊCH (Dùng Service)
            var giangVienId = lhp.GiangVienId.GetValueOrDefault();
            var lichTrung = await _lhpService.CheckLichTrungAsync(new List<LichHocDTO> { scheduleToCreate }, giangVienId, dto.LopHocPhanId);

            if (lichTrung.Any())
            {
                ModelState.AddModelError("", $"Lỗi trùng lịch học. Lịch này trùng lịch giảng viên hoặc phòng học.");
                return View(dto);
            }

            // 7. GỌI API TẠO 1 LỊCH (API call giữ nguyên)
            var (success, data, error) = await CallApiAsync("api/lophocphan/lichhoc", HttpMethod.Post, scheduleToCreate);

            if (success)
            {
                TempData["Success"] = $"Thêm lịch học thành công vào ngày {dto.Ngay.ToString("dd/MM/yyyy")}!";
                return RedirectToAction(nameof(CourseClassDetail), new { id = dto.LopHocPhanId });
            }

            ModelState.AddModelError("", error ?? "Lỗi API khi tạo lịch học thủ công.");
            return View(dto);
        }

        #endregion

        #region ======= Auto Add Schedule  =======

        public async Task<IActionResult> AutoAddSchedule(int lhpId)
        {
            var lhp = await _lhpService.GetLopHocPhanDetailsAsync(lhpId);

            if (lhp == null)
            {
                TempData["Error"] = "Không tìm thấy Lớp Học Phần.";
                return RedirectToAction(nameof(CourseClassManager));
            }

            // 1. Gán ViewBag cần thiết
            ViewBag.LHP = lhp;

            // Tải danh sách phòng học đã lọc theo môn học (Logic nghiệp vụ)
            var phongHocs = await _lhpService.GetPhongHocListAsync(lhp.MonHoc.Id);
            ViewBag.PhongHocList = new SelectList(phongHocs, "Id", "MaPhongHoc");

            // Gán các helper maps
            ViewBag.DaysOfWeek = new List<object>
    {
        new { Id = 2, Name = "Thứ Hai" }, new { Id = 3, Name = "Thứ Ba" }, new { Id = 4, Name = "Thứ Tư" },
        new { Id = 5, Name = "Thứ Năm" }, new { Id = 6, Name = "Thứ Sáu" }, new { Id = 7, Name = "Thứ Bảy" },
        new { Id = 8, Name = "Chủ Nhật" }
    };
            ViewBag.TietStartMap = _lhpService.GetTietStartMap();
            ViewBag.TietEndMap = _lhpService.GetTietEndMap();

            // Gán thông tin giảng viên
            ViewBag.GiangVien = $"{lhp.GiangVien.HoVaTenDem} {lhp.GiangVien.Ten} (Mã: {lhp.GiangVien.MaGiangVien})";


            // 2. Return View
            return View(new AutoAddScheduleDTO
            {
                LopHocPhanId = lhp.Id,
                TietBatDau = 2,
                TietKetThuc = 6
            });
        }

        // Trong LopHocPhanController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoAddSchedule(AutoAddScheduleDTO dto)
        {
            var lhp = await _lhpService.GetLopHocPhanDetailsAsync(dto.LopHocPhanId);

            // GÁN MAP TIẾT HỌC CHO TRƯỜNG HỢP LỖI
            ViewBag.TietStartMap = _lhpService.GetTietStartMap();
            ViewBag.TietEndMap = _lhpService.GetTietEndMap();

            // 1. Validation cơ bản (Model, Days, LHP)
            if (!ModelState.IsValid || dto.CacNgayTrongTuan == null || !dto.CacNgayTrongTuan.Any() || lhp == null)
            {
                ModelState.AddModelError("", "Lỗi nhập liệu: Vui lòng chọn ít nhất một ngày và điền đủ thông tin.");
                return RePopulateViewAndReturn(dto, lhp);
            }

            // 2. Validation Tiết học
            if (dto.TietBatDau > dto.TietKetThuc)
            {
                ModelState.AddModelError(nameof(dto.TietKetThuc), "Tiết kết thúc phải lớn hơn hoặc bằng Tiết bắt đầu.");
                return RePopulateViewAndReturn(dto, lhp);
            }

            // 3. CHUYỂN ĐỔI TIẾT SANG TIMESPAN VÀ XÁC ĐỊNH SỐ BUỔI CẦN THIẾT
            var (startTime, endTime) = _lhpService.ConvertTietToTimeSpan(dto.TietBatDau, dto.TietKetThuc);

            // Khai báo ràng buộc số buổi tối đa (9/6)
            var isThucHanh = lhp.MonHoc.MoTa?.ToUpper().Contains("TH") ?? false;
            const int MAX_LY_THUYET_BUOI = 9;
            const int MAX_THUC_HANH_BUOI = 6;
            int maxAllowedSessions = isThucHanh ? MAX_THUC_HANH_BUOI : MAX_LY_THUYET_BUOI;

            // Tính số buổi còn lại cần tạo (đã bao gồm tự động dừng tạo)
            int existingSessions = await _context.LichHocs.CountAsync(l => l.LopHocPhanId == dto.LopHocPhanId);
            int remainingSessions = maxAllowedSessions - existingSessions;

            // 3.1. Kiểm tra: Lớp đã đủ lịch rồi thì dừng
            if (remainingSessions <= 0)
            {
                TempData["Error"] = $"Lớp học phần này đã có đủ {existingSessions}/{maxAllowedSessions} buổi học. Không cần tạo thêm.";
                return RedirectToAction(nameof(CourseClassDetail), new { id = dto.LopHocPhanId });
            }

            // 4. CHUẨN BỊ VÀ LỌC DANH SÁCH LỊCH HỌC TẠM THỜI (Tích hợp logic tự động dừng và lọc tuần)
            var schedulesToCreate = new List<LichHocDTO>();

            // Lặp qua tất cả các ngày trong khoảng thời gian LHP
            foreach (var date in Enumerable.Range(0, (lhp.NgayKetThuc - lhp.NgayBatDau).Days + 1)
                .Select(i => lhp.NgayBatDau.AddDays(i)))
            {
                // TỰ ĐỘNG DỪNG TẠO LỊCH KHI ĐỦ SỐ BUỔI CẦN THIẾT
                if (schedulesToCreate.Count >= remainingSessions)
                {
                    break;
                }

                // A. Lọc theo Ngày trong tuần đã chọn
                if (dto.CacNgayTrongTuan.Contains(_lhpService.ConvertDayOfWeekToCustomDay(date.DayOfWeek)))
                {
                    bool isValidDay = true;

                    // B. Lọc theo Tuần chẵn/lẻ (chỉ cho môn Thực hành)
                    if (isThucHanh)
                    {
                        var daysElapsed = (date - lhp.NgayBatDau).TotalDays;
                        int weekCount = (int)Math.Floor(daysElapsed / 7.0);
                        // Chỉ chấp nhận tuần 1, 3, 5, ... (weekCount = 0, 2, 4, ...)
                        if ((weekCount % 2) != 0)
                        {
                            isValidDay = false; // Bỏ qua tuần lẻ
                        }
                    }

                    // C. Thêm vào danh sách nếu hợp lệ
                    if (isValidDay)
                    {
                        schedulesToCreate.Add(new LichHocDTO
                        {
                            LopHocPhanId = dto.LopHocPhanId,
                            Ngay = date,
                            GioBatDau = startTime,
                            GioKetThuc = endTime,
                            PhongHocId = dto.PhongHocId
                        });
                    }
                }
            }
            // ----------------------------------------------------------------------

            // 5. RÀNG BUỘC NGHIỆP VỤ: KIỂM TRA ĐỦ SỐ BUỔI CẦN THIẾT HAY KHÔNG
            // Kiểm tra xem khoảng thời gian LHP có TẠO RA ĐỦ số buổi cần thiết không.
            if (schedulesToCreate.Count < remainingSessions)
            {
                string monHocType = isThucHanh ? "Thực Hành" : "Lý Thuyết";
                string errorMessage = $"Lỗi: Không thể tạo đủ {remainingSessions} buổi. Khoảng thời gian LHP chỉ tạo ra được {schedulesToCreate.Count} buổi hợp lệ. Vui lòng kiểm tra lại ngày bắt đầu/kết thúc LHP.";

                ModelState.AddModelError("", errorMessage);
                return RePopulateViewAndReturn(dto, lhp);
            }

            // 6. VALIDATION PHÒNG HỌC (Server-side check)
            var validPhongHocs = await _lhpService.GetPhongHocListAsync(lhp.MonHoc.Id);
            if (!validPhongHocs.Any(ph => ph.Id == dto.PhongHocId))
            {
                ModelState.AddModelError(nameof(dto.PhongHocId), "Phòng học được chọn không phù hợp với loại môn học (Thực hành/Lý thuyết).");
                return RePopulateViewAndReturn(dto, lhp);
            }

            // 7. KIỂM TRA TRÙNG LỊCH (Giữ nguyên)
            var giangVienId = lhp.GiangVienId.GetValueOrDefault();
            var lichTrung = await _lhpService.CheckLichTrungAsync(schedulesToCreate, giangVienId);

            if (lichTrung.Any())
            {
                ModelState.AddModelError("", "Lỗi trùng lịch học. Vui lòng kiểm tra các ngày và phòng học sau:");

                foreach (var lich in lichTrung)

                {

                    ModelState.AddModelError("", $"- GV {lhp.GiangVien.MaGiangVien} trùng lịch vào {lich.Ngay.ToString("dd/MM/yyyy")} ({lich.GioBatDau} - {lich.GioKetThuc}) tại Phòng ID {lich.PhongHocId}");

                }
                return RePopulateViewAndReturn(dto, lhp);
            }

            // 8. GỌI API BATCH
            var (success, data, error) = await CallApiAsync("api/lophocphan/lichhoc/batch", HttpMethod.Post, schedulesToCreate);

            if (success)
            {
                TempData["Success"] = $"Tạo **{schedulesToCreate.Count}** lịch học tự động thành công cho LHP **{lhp.MaLopHocPhan}**!";
                return RedirectToAction(nameof(CourseClassDetail), new { id = dto.LopHocPhanId });
            }

            ModelState.AddModelError("", error ?? "Lỗi API khi tạo hàng loạt lịch học.");
            return RePopulateViewAndReturn(dto, lhp);
        }

        #endregion

        #region ======= Other Actions (Update/Delete) =======

        [HttpPost]
        public async Task<IActionResult> UpdateCourseClassStatus(int lopHocPhanId, int trangThaiId, string? lyDo = null)
        {
            var dto = new UpdateLopHocPhanStatusDTO { LopHocPhanId = lopHocPhanId, TrangThaiId = trangThaiId, LyDo = lyDo };

            // 1. Gọi API để cập nhật trạng thái
            var (success, data, error) = await CallApiAsync("api/lophocphan/trangthai", HttpMethod.Put, dto);

            string message;

            if (success)
            {
                // 2. Xử lý thành công: Lấy message từ JSON data hoặc gán mặc định
                if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("message", out var messageElement))
                {
                    message = messageElement.GetString() ?? "Cập nhật trạng thái thành công.";
                }
                else
                {
                    message = "Cập nhật trạng thái thành công.";
                }
            }
            else
            {
                // 3. Xử lý lỗi: Lấy message từ error (từ CallApiAsync) hoặc gán mặc định
                message = error ?? "Cập nhật trạng thái không thành công.";

                // Cố gắng parse message từ API nếu nó là JSON lỗi (ví dụ: { "message": "..." })
                try
                {
                    if (error.Contains("API error:") && error.Contains("{"))
                    {
                        var jsonError = error.Substring(error.IndexOf('{'));
                        dynamic errObj = JsonConvert.DeserializeObject(jsonError);
                        message = errObj?.message ?? message;
                    }
                }
                catch { /* Bỏ qua nếu parse lỗi */ }
            }

            // 4. Trả về JSON với message đã được xử lý
            return Json(new { success, message });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteCourseClass(int lopHocPhanId)
        {
            try
            {
                // Dùng Service để xóa
                bool success = await _lhpService.DeleteCourseClassAsync(lopHocPhanId);

                if (success)
                    return Json(new { success = true, message = "Xóa lớp học phần thành công." });
                else
                    return Json(new { success = false, message = "Không tìm thấy lớp học phần." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa LHP ID {Id}", lopHocPhanId);
                return Json(new { success = false, message = "Lỗi hệ thống khi xóa: " + ex.Message });
            }
        }
        #endregion

        #region ======= Get Giang Vien / Generate LHP Info / Get Hoc Ky Dates =======

        [HttpGet]
        public async Task<IActionResult> GetGiangVienTheoMon(int? monHocId, string search = "")
        {
            var giangViens = await _lhpService.GetGiangVienByMonHocAsync(monHocId, search);

            var result = giangViens
                .Select(gv => new { Id = gv.Id, HoTen = gv.HoVaTenDem + " " + gv.Ten })
                .ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GenerateLopHocPhanInfo(int hocKyId, int monHocId)
        {
            if (hocKyId <= 0 || monHocId <= 0)
            {
                return Json(new { success = false, message = "Thiếu Học kỳ hoặc Môn học ID." });
            }

            try
            {
                // Dùng Service để sinh Mã LHP
                var maLopHocPhan = await _lhpService.GenerateAutoMaLHPAsync(hocKyId, monHocId);

                // ... (Logic tìm tên môn học cũ)
                var monHoc = await _context.MonHocs.FindAsync(monHocId);
                string tenGoiY = monHoc != null ? monHoc.TenMonHoc : "";

                return Json(new
                {
                    success = true,
                    maLopHocPhan = maLopHocPhan,
                    tenLopHocPhan = tenGoiY
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi sinh thông tin LHP tự động");
                return Json(new { success = false, message = "Lỗi server khi sinh mã." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHocKyDates(int hocKyId)
        {
            // Dùng Service để lấy ngày
            var (success, ngayBatDau, ngayKetThuc, error) = await _lhpService.GetHocKyDatesAsync(hocKyId);

            if (!success)
            {
                return Json(new { success = false, message = error });
            }

            // Trả về ngày dưới dạng chuỗi format 'yyyy-MM-dd'
            return Json(new
            {
                success = true,
                ngayBatDau = ngayBatDau.ToString("yyyy-MM-dd"),
                ngayKetThuc = ngayKetThuc.ToString("yyyy-MM-dd")
            });
        }

        #endregion



        #region ======= Tu dong tao lop va dang ky =======


        private async Task<List<HocKy>> GetFilteredHocKyListFromDb()
        {

            var validStatusNames = new List<string> { "Mới tạo", "Đang diễn ra" };
            var validStatusIds = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "HocKy" && validStatusNames.Contains(t.TenTrangThai))
                .Select(t => t.Id)
                .ToListAsync();

            return await _context.HocKys
                .Where(hk => validStatusIds.Contains(hk.TrangThaiId))
                .OrderByDescending(hk => hk.NgayBatDau)
                .ToListAsync();
        }
        private async Task<int> GetRandomPhongHocId()
        {
            var phongList = await _lhpService.GetPhongHocListAsync(null);
            if (!phongList.Any()) return 1; // Default to 1 if no rooms found

            var random = new Random();
            int index = random.Next(phongList.Count);
            return phongList[index].Id;
        }

        private async Task LoadAutoEnrollmentViewBags()
        {

            var hocKys = await GetFilteredHocKyListFromDb();
            ViewBag.HocKyList = new SelectList(hocKys, "Id", "TenHocKy");

            ViewBag.NganhList = await _context.NganhHocs
                .Include(n => n.Khoa)
                .OrderBy(n => n.TenNganh)
                .ToListAsync();

            int currentYear = DateTime.Now.Year;
            ViewBag.KhoaNhapHocList = Enumerable.Range(currentYear - 5, 6).Reverse().ToList();


        }
        [HttpGet]
        public async Task<IActionResult> AutoCreateClassAndEnroll()
        {
            await LoadAutoEnrollmentViewBags();

            return View(new AutoEnrollmentRequestDTO());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoCreateClassAndEnroll(AutoEnrollmentRequestDTO request)
        {
            // 1. Validate dữ liệu từ Form View (Chỉ chứa các trường CTĐT)
            if (!ModelState.IsValid)
            {
                await LoadAutoEnrollmentViewBags();
                return View(request);
            }

            // 2. Lấy danh sách Môn học bắt buộc
            var mandatorySubjects = await _lhpService.GetMandatorySubjectsForAutoClassCreationAsync(
                request.NganhId, request.KhoaNhapHoc, request.ThuTuHocKy);

            if (!mandatorySubjects.Any())
            {
                TempData["Error"] = $"Không tìm thấy môn học bắt buộc nào trong CTĐT cho Ngành ID {request.NganhId}, Khóa {request.KhoaNhapHoc}, Kỳ {request.ThuTuHocKy}.";
                return RedirectToAction(nameof(CourseClassManager));
            }

            // --- BƯỚC QUAN TRỌNG: SINH DỮ LIỆU LỊCH HỌC NGẪU NHIÊN TẠI ĐÂY ---
            var (randomTietBatDau, randomTietKetThuc, _) = _lhpService.GetRandomScheduleSettings();
            int randomPhongHocId = await GetRandomPhongHocId();

            // 2. Gửi TOÀN BỘ ngày trong tuần để Service có thể chọn bất kỳ ngày nào
            var fullWeekDays = new List<int> { 2, 3, 4, 5, 6, 7 };

            var apiPayload = new AutoEnrollmentApiPayload
            {
                HocKyId = request.HocKyId,
                NganhId = request.NganhId,
                KhoaNhapHoc = request.KhoaNhapHoc,
                ThuTuHocKy = request.ThuTuHocKy,

                ShouldAutoCreateSchedule = true,

                // Các giá trị này chỉ là "giá trị mồi" để qua mặt Validation.
                // Service sẽ KHÔNG dùng cứng nhắc các giá trị này mà sẽ Random lại.
                DefaultPhongHocId = randomPhongHocId,
                TietBatDau = randomTietBatDau,
                TietKetThuc = randomTietKetThuc,

                // Quan trọng: Gửi full tuần
                CacNgayTrongTuan = fullWeekDays,

                MandatorySubjectCodes = mandatorySubjects.Select(s => s.MaMonHoc).ToList()
            };

            // 4. Gọi API
            var (success, data, error) = await CallApiAsync("api/auto-create-enroll", HttpMethod.Post, apiPayload);

            if (!success)
            {
                // Xử lý hiển thị lỗi chi tiết từ API
                string errorMsg = error;
                try
                {
                    if (error.Contains("API error:") && error.Contains("{"))
                    {
                        // Cắt chuỗi để lấy phần JSON
                        var jsonPart = error.Substring(error.IndexOf('{'));
                        dynamic errObj = JsonConvert.DeserializeObject(jsonPart);

                        // Kiểm tra xem có property "errors" (Validation Error) không
                        if (errObj?.errors != null)
                        {
                            errorMsg = "Lỗi dữ liệu: ";
                            foreach (var err in errObj.errors)
                            {
                                errorMsg += $"{err.Name}: {err.Value[0]} ";
                            }
                        }
                        else
                        {
                            errorMsg = errObj?.message ?? error;
                        }
                    }
                }
                catch { }

                TempData["Error"] = errorMsg;

                await LoadAutoEnrollmentViewBags();
                return View(request);
            }
            else
            {
                string message = "Đăng ký tự động thành công.";
                try
                {
                    if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("message", out var msgProp))
                    {
                        message = msgProp.GetString();
                    }
                }
                catch { }

                TempData["Success"] = message;
            }

            return RedirectToAction(nameof(CourseClassManager));
        }
        #endregion


        //Trang quản lý môn học
        public async Task<IActionResult> SubjectManager(string? keyword)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl);

                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                HttpResponseMessage response =
                    await client.GetAsync("api/laydanhsachmonhoc");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không thể lấy danh sách môn học!";
                    return View(new List<MonHocViewModel>());
                }

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                var root = doc.RootElement;

                var data = root.GetProperty("data").GetRawText();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var subjects = JsonSerializer.Deserialize<List<MonHocViewModel>>(data, options);

                // Lọc theo keyword
                if (!string.IsNullOrEmpty(keyword))
                {
                    subjects = subjects
                        .Where(s =>
                            s.MaMonHoc.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            s.TenMonHoc.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        )
                        .ToList();

                    ViewBag.Keyword = keyword;
                }

                ViewBag.Tong = subjects.Count;

                return View(subjects);
            }
            catch
            {
                TempData["Error"] = "Lỗi kết nối API!";
                return View(new List<MonHocViewModel>());
            }
        }




        //Trang chi tiết môn học
        public async Task<IActionResult> SubjectDetails(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl);

                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.GetAsync($"api/monhocchitiet/{id}");
                var body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.GetProperty("result").GetBoolean() == false)
                {
                    TempData["Error"] = root.GetProperty("message").GetString();
                    return RedirectToAction("SubjectManager");
                }

                var dataJson = root.GetProperty("data").GetRawText();

                var detail = JsonSerializer.Deserialize<SubjectDetailViewModel>(
                    dataJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return View(detail);
            }
            catch
            {
                TempData["Error"] = "Không thể kết nối API!";
                return RedirectToAction("SubjectManager");
            }
        }



        //Trang thêm môn học
        [HttpGet]
        public IActionResult AddSubject()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddSubject(MonHocCreate model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl);

                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/taomonhoc", content);
                var body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                bool result = root.GetProperty("result").GetBoolean();
                string message = root.GetProperty("message").GetString();

                if (!result)
                {
                    TempData["Error"] = message;
                    return View(model);
                }

                TempData["Success"] = "Thêm môn học thành công!";
                return RedirectToAction("SubjectManager");
            }
            catch
            {
                TempData["Error"] = "Không thể kết nối API!";
                return View(model);
            }
        }

        //Xóa môn học 
        [HttpPost]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl);

                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.DeleteAsync($"api/xoamonhoc/{id}");
                var body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                bool result = root.GetProperty("result").GetBoolean();
                string message = root.GetProperty("message").GetString();

                if (!result)
                {
                    TempData["Error"] = message;
                    return RedirectToAction("SubjectManager");
                }

                TempData["Success"] = "Xóa môn học thành công!";
                return RedirectToAction("SubjectManager");
            }
            catch
            {
                TempData["Error"] = "Không thể kết nối API!";
                return RedirectToAction("SubjectManager");
            }
        }

        //Trang sửa môn
        [HttpGet]
        public async Task<IActionResult> EditSubject(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl);

                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                // Gọi API lấy thông tin môn học
                var response = await client.GetAsync($"api/monhoc/{id}");
                var body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (!root.GetProperty("result").GetBoolean())
                {
                    TempData["Error"] = "Không tìm thấy môn học!";
                    return RedirectToAction("SubjectManager");
                }

                var jsonData = root.GetProperty("data").GetRawText();

                var model = JsonSerializer.Deserialize<MonHocUpdate>(
                    jsonData,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Không thể kết nối API!";
                return RedirectToAction("SubjectManager");
            }
        }



        public async Task<IActionResult> EditSubject(int id, MonHocUpdate model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl);

                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"api/suamonhoc/{id}", content);
                var body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                bool result = root.GetProperty("result").GetBoolean();
                string message = root.GetProperty("message").GetString();

                if (!result)
                {
                    TempData["Error"] = message;
                    return View(model);
                }

                TempData["Success"] = "Cập nhật môn học thành công!";
                return RedirectToAction("SubjectDetails", new { id = id });
            }
            catch
            {
                TempData["Error"] = "Không thể kết nối API!";
                return View(model);
            }
        }

        //Gán môn học cho ngành
        [HttpGet]
        public async Task<IActionResult> AssignNganh(int id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Lấy thông tin môn học (để biết ngành nào đang áp dụng)
            var responseSubject = await client.GetAsync($"api/monhocchitiet/{id}");
            var jsonSubject = await responseSubject.Content.ReadAsStringAsync();

            var subjectData = JsonDocument.Parse(jsonSubject)
                .RootElement.GetProperty("data").GetRawText();

            var subject = JsonSerializer.Deserialize<SubjectDetailViewModel>(subjectData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Lấy toàn bộ ngành
            var responseNganh = await client.GetAsync("api/laydanhsachnganhhoc");
            var jsonNganh = await responseNganh.Content.ReadAsStringAsync();
            var dataNganh = JsonDocument.Parse(jsonNganh)
                .RootElement.GetProperty("data").GetRawText();

            var nganhList = JsonSerializer.Deserialize<List<NganhHoc>>(dataNganh,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            ViewBag.AllNganhs = nganhList;
            return View(subject);
        }


        [HttpPost]
        public async Task<IActionResult> AssignNganh(int id, int[] selectedNganhs)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var body = JsonSerializer.Serialize(new { NganhIds = selectedNganhs });
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"api/gannhommonghoc/{id}", content);
            var json = await response.Content.ReadAsStringAsync();

            var root = JsonDocument.Parse(json).RootElement;

            if (!root.GetProperty("result").GetBoolean())
            {
                TempData["Error"] = root.GetProperty("message").GetString();
                return RedirectToAction("AssignNganh", new { id });
            }

            TempData["Success"] = "Gán ngành cho môn thành công!";
            return RedirectToAction("SubjectDetails", new { id });
        }

        //Phân công GV dạy môn
        [HttpGet]
        public async Task<IActionResult> AssignGiangVien(int id, string search)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var subjectResponse = await client.GetAsync($"api/monhocchitiet/{id}");
            if (!subjectResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể tải dữ liệu môn học!";
                return RedirectToAction("SubjectManager");
            }

            var subjectJson = await subjectResponse.Content.ReadAsStringAsync();
            var subjectData = JsonDocument.Parse(subjectJson).RootElement.GetProperty("data");

            var subject = JsonSerializer.Deserialize<SubjectDetailViewModel>(
                subjectData.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // Gọi API giảng viên
            var gvResponse = await client.GetAsync("api/laydanhsachgiangvien");
            if (!gvResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể tải danh sách giảng viên!";
                return RedirectToAction("SubjectDetails", new { id });
            }

            var gvJson = await gvResponse.Content.ReadAsStringAsync();
            var gvs = JsonSerializer.Deserialize<List<GiangVien>>(
                JsonDocument.Parse(gvJson).RootElement.GetProperty("data").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // 🔍 Lọc nếu có từ khóa
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                gvs = gvs.Where(gv =>
                    ($"{gv.HoVaTenDem} {gv.Ten}").ToLower().Contains(search) ||
                    gv.MaGiangVien.ToLower().Contains(search)
                ).ToList();
            }

            ViewBag.AllGiangVien = gvs;
            return View(subject);
        }


        [HttpPost]
        public async Task<IActionResult> AssignGiangVien(int id, List<int> selectedGVs)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PostAsJsonAsync(
                $"api/gan-giang-vien-mon/{id}", selectedGVs);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Cập nhật giảng viên thành công!";
                return RedirectToAction("SubjectDetails", new { id });
            }

            TempData["Error"] = "Gán giảng viên thất bại!";
            return RedirectToAction("AssignGiangVien", new { id });
        }


        // GET: Quản lý ngành
        [HttpGet]
        public async Task<IActionResult> MajorManager(string keyword, int? khoaId)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Gọi danh sách khoa để lọc
            var khoaResponse = await client.GetAsync("api/laydanhsachkhoa");
            var khoaData = JsonDocument.Parse(await khoaResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            var khoas = JsonSerializer.Deserialize<List<Khoa>>(khoaData.ToString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            ViewBag.Khoas = khoas;

            // Gọi danh sách ngành
            var response = await client.GetAsync("api/laydanhsachnganhhoc");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể tải danh sách ngành!";
                return View(new List<NganhHoc>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json).RootElement.GetProperty("data");
            var nganhList = JsonSerializer.Deserialize<List<NganhHoc>>(data.ToString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                nganhList = nganhList.Where(n =>
                    n.MaNganh.ToLower().Contains(keyword) ||
                    n.TenNganh.ToLower().Contains(keyword)).ToList();
            }

            if (khoaId.HasValue)
            {
                nganhList = nganhList.Where(n => n.KhoaId == khoaId).ToList();
            }

            ViewBag.Keyword = keyword;
            ViewBag.KhoaId = khoaId;

            return View(nganhList);
        }


        //Trang thêm ngành học
        [HttpGet]
        public IActionResult AddMajor()
        {
            var khoas = _context.Khoas.OrderBy(k => k.TenKhoa).ToList();
            ViewBag.Khoas = khoas;

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> AddMajor(NganhHoc model)
        {
            // Gán lại danh sách khoa khi submit thất bại
            var khoas = _context.Khoas.OrderBy(k => k.TenKhoa).ToList();
            ViewBag.Khoas = khoas;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return View(model);
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PostAsJsonAsync("api/them-nganhhoc", model);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Thêm ngành học thành công.";
                return RedirectToAction("MajorManager");
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Lỗi khi thêm ngành: {errorMsg}";

            return View(model);
        }

        //Xem chi tiết ngành học
        public IActionResult MajorDetails(int id)
        {
            // Lấy ngành học, bao gồm cả thông tin khoa và các môn học liên quan
            var major = _context.NganhHocs
                .Include(n => n.Khoa)
                .Include(n => n.MonHocs)
                .FirstOrDefault(n => n.Id == id);

            if (major == null)
            {
                TempData["Error"] = "Không tìm thấy ngành học.";
                return RedirectToAction("MajorManager");
            }

            return View("MajorDetails", major);
        }

        //Sửa ngành học
        [HttpGet]
        public IActionResult EditMajor(int id)
        {
            var major = _context.NganhHocs.Find(id);

            if (major == null)
            {
                return NotFound();
            }

            // Lấy danh sách khoa để hiển thị dropdown
            ViewBag.Khoas = _context.Khoas.ToList();

            return View(major);
        }

        [HttpPost]
        public IActionResult EditMajor(NganhHoc model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Khoas = _context.Khoas.ToList();
                return View(model);
            }

            var existing = _context.NganhHocs.Find(model.Id);

            if (existing == null)
            {
                return NotFound();
            }

            // Kiểm tra khoa có tồn tại không
            var khoa = _context.Khoas.Find(model.KhoaId);
            if (khoa == null)
            {
                ModelState.AddModelError("KhoaId", "Khoa không tồn tại!");
                ViewBag.Khoas = _context.Khoas.ToList();
                return View(model);
            }

            // Cập nhật dữ liệu
            existing.MaNganh = model.MaNganh;
            existing.TenNganh = model.TenNganh;
            existing.KhoaId = model.KhoaId;

            _context.SaveChanges();

            TempData["Success"] = "Cập nhật ngành học thành công!";
            return RedirectToAction("MajorDetails", new { id = model.Id }); // 🔁 Chuyển về trang chi tiết
        }



        //Xóa ngành học
        [HttpPost]
        public IActionResult DeleteMajor(int id)
        {
            var major = _context.NganhHocs
                .Include(n => n.MonHocs) // load các môn học liên quan (nếu có)
                .FirstOrDefault(n => n.Id == id);

            if (major == null)
            {
                return NotFound();
            }

            // Xóa mối liên hệ giữa ngành và môn học nếu có
            if (major.MonHocs != null && major.MonHocs.Any())
            {
                major.MonHocs.Clear(); // EF Core sẽ tự xóa ở bảng trung gian nếu dùng Many-to-Many
            }

            _context.NganhHocs.Remove(major);
            _context.SaveChanges();

            TempData["Success"] = "Xóa ngành học thành công!";
            return RedirectToAction("MajorManager");
        }

        //Gán môn học cho ngành
        [HttpGet]
        public async Task<IActionResult> AssignMonHoc(int id, string keyword)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Lấy chi tiết ngành
            var resNganh = await client.GetAsync($"api/laychitietnganhhoc/{id}");
            var jsonNganh = await resNganh.Content.ReadAsStringAsync();
            var nganh = JsonSerializer.Deserialize<NganhHoc>(
                JsonDocument.Parse(jsonNganh).RootElement.GetProperty("data").ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Lấy toàn bộ môn học
            var resMon = await client.GetAsync("api/laydanhsachmonhoc");
            var jsonMon = await resMon.Content.ReadAsStringAsync();
            var allSubjects = JsonSerializer.Deserialize<List<MonHoc>>(
                JsonDocument.Parse(jsonMon).RootElement.GetProperty("data").ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Lọc nếu có keyword
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                allSubjects = allSubjects
                    .Where(m => m.TenMonHoc.ToLower().Contains(keyword) || m.MaMonHoc.ToLower().Contains(keyword))
                    .ToList();
            }

            ViewBag.AllSubjects = allSubjects;
            ViewBag.Keyword = keyword;
            return View(nganh);
        }



        [HttpPost]
        public async Task<IActionResult> AssignMonHoc(int id, int[] selectedSubjects)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var payload = JsonSerializer.Serialize(new { MonHocIds = selectedSubjects });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"api/ganmonchonganh/{id}", content);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể cập nhật môn học cho ngành!";
                return RedirectToAction("AssignMonHoc", new { id });
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(responseJson).RootElement;

            if (!root.GetProperty("result").GetBoolean())
            {
                TempData["Error"] = root.GetProperty("message").GetString();
                return RedirectToAction("AssignMonHoc", new { id });
            }

            TempData["Success"] = "Gán môn học cho ngành thành công!";
            return RedirectToAction("MajorDetails", new { id });
        }

        // Gán môn học cho giảng viên
        [HttpGet]
        public async Task<IActionResult> AssignSubjectsToTeacher(string id)
        {
            // id lúc này là MaGiangVien (string)

            var giangVien = await _context.GiangViens
                .Include(gv => gv.GiangVienMonHocs)
                .FirstOrDefaultAsync(gv => gv.MaGiangVien == id);

            if (giangVien == null)
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return RedirectToAction("TeacherManager");
            }

            var monHocs = await _context.MonHocs.ToListAsync();

            // Gán thủ công để hiển thị tên môn học trong view
            foreach (var gvmh in giangVien.GiangVienMonHocs)
            {
                gvmh.MonHoc = monHocs.FirstOrDefault(m => m.Id == gvmh.MonHocId);
            }

            ViewBag.AllSubjects = monHocs;
            return View(giangVien);
        }



        [HttpPost]
        public async Task<IActionResult> AssignSubjectsToTeacher(string id, int[] selectedSubjects)
        {
            if (selectedSubjects == null)
                selectedSubjects = new int[0];

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("access_token");

            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var content = new StringContent(
                JsonSerializer.Serialize(selectedSubjects),
                Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"api/gan-mon-giang-vien/{id}", content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(json))
            {
                TempData["Error"] = $"API không phản hồi hợp lệ. {(int)response.StatusCode} - {response.ReasonPhrase}";
                return RedirectToAction("AssignSubjectsToTeacher", new { id });
            }

            var result = JsonDocument.Parse(json).RootElement;

            if (!result.GetProperty("result").GetBoolean())
            {
                TempData["Error"] = result.GetProperty("message").GetString();
                return RedirectToAction("TeacherDetails", new { maGiangVien = id });
            }

            TempData["Success"] = "Gán môn phụ trách thành công!";
            return RedirectToAction("TeacherDetails", new { maGiangVien = id });
        }

        // Quản lý Cơ sở (FacilityManager)

        public async Task<IActionResult> FacilityManager(string keyword)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            ViewBag.AccessToken = token;

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                HttpResponseMessage response = await client.GetAsync("api/laydanhsachcoso");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể lấy danh sách Cơ sở (API Error).";
                    return View(new List<CoSo>());
                }

                var body = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                List<CoSo> listCoSoFull = new List<CoSo>();

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataElement))
                {
                    listCoSoFull = JsonSerializer.Deserialize<List<CoSo>>(
                        dataElement.ToString(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                else
                {
                    try
                    {
                        listCoSoFull = JsonSerializer.Deserialize<List<CoSo>>(
                            body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    catch { }
                }
                if (listCoSoFull == null)
                    listCoSoFull = new List<CoSo>();

                var query = listCoSoFull.AsQueryable();

                if (!string.IsNullOrEmpty(keyword))
                {
                    var kw = keyword.Trim().ToLower();
                    query = query.Where(c =>
                        c.TenCoSo.ToLower().Contains(kw) ||
                        c.DiaChi.ToLower().Contains(kw)
                    );

                    ViewData["CurrentFilter"] = keyword;
                }

                return View(query.ToList());
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi kết nối: " + ex.Message;
                return View(new List<CoSo>());
            }
        }
        private string GetAccessToken()
        {
            return HttpContext.Session.GetString("access_token") ?? "";
        }

        [HttpGet]
        public async Task<IActionResult> FacilityDetails(int id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = GetAccessToken();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            ViewBag.AccessToken = token;

            try
            {
                var response = await client.GetAsync($"api/laychitietcoso?id={id}");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không lấy được chi tiết cơ sở.";
                    return RedirectToAction(nameof(FacilityManager));
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                CoSo coSo = null;

                if (root.TryGetProperty("data", out var dataElement))
                {
                    coSo = JsonSerializer.Deserialize<CoSo>(dataElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                if (coSo == null)
                {
                    TempData["Error"] = "Không tìm thấy cơ sở.";
                    return RedirectToAction(nameof(FacilityManager));
                }

                return View(coSo);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi kết nối API: " + ex.Message;
                return RedirectToAction(nameof(FacilityManager));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditFacility(int id)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = GetAccessToken();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            ViewBag.AccessToken = token;

            var response = await client.GetAsync($"api/laychitietcoso?id={id}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không tìm thấy Cơ sở cần sửa.";
                return RedirectToAction(nameof(FacilityManager));
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var data = root.GetProperty("data").GetRawText();

            var model = System.Text.Json.JsonSerializer.Deserialize<CoSo>(data, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFacility(int id, CoSo model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return View(model);
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            model.Id = id;
            var json = System.Text.Json.JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"api/capnhat-coso/{id}", content);
            var body = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            bool result = root.GetProperty("result").GetBoolean();
            string message = root.GetProperty("message").GetString();

            if (!result)
            {
                TempData["Error"] = message;
                return View(model);
            }

            TempData["Success"] = "Cập nhật cơ sở thành công!";
            return RedirectToAction(nameof(FacilityDetails), new { id = model.Id });
        }
        private HttpClient CreateHttpClientWithAuth()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        [HttpGet]
        public async Task<IActionResult> AddRoom(int coSoId)
        {
            var coSo = await _context.CoSos.FindAsync(coSoId);
            if (coSo == null)
            {
                TempData["Error"] = "Không tìm thấy cơ sở để thêm phòng học.";
                return RedirectToAction(nameof(FacilityManager));
            }

            var model = new PhongHoc { CoSoId = coSoId };
            ViewBag.CoSo = coSo;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoom(PhongHoc model)
        {
            var coSo = await _context.CoSos.FindAsync(model.CoSoId);
            if (coSo == null)
            {
                TempData["Error"] = "Không tìm thấy cơ sở.";
                return RedirectToAction(nameof(FacilityManager));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CoSo = coSo;
                return View(model);
            }

            try
            {
                var client = CreateHttpClientWithAuth();
                var response = await client.PostAsJsonAsync("api/taomoi-phonghoc", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Thêm phòng học thành công!";
                    return RedirectToAction(nameof(FacilityDetails), new { id = model.CoSoId });
                }

                var errMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Lỗi API khi thêm phòng học: {errMsg}";
                ViewBag.CoSo = coSo;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                ViewBag.CoSo = coSo;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditRoom(int id)
        {
            var phongHoc = await _context.PhongHocs.Include(p => p.CoSo)
                                                   .FirstOrDefaultAsync(p => p.Id == id);
            if (phongHoc == null)
            {
                TempData["Error"] = "Không tìm thấy phòng học.";
                return RedirectToAction(nameof(FacilityManager));
            }

            ViewBag.CoSo = phongHoc.CoSo;
            return View(phongHoc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoom(int id, PhongHoc model)
        {
            var coSo = await _context.CoSos.FindAsync(model.CoSoId);
            if (coSo == null)
            {
                TempData["Error"] = "Không tìm thấy cơ sở.";
                return RedirectToAction(nameof(FacilityManager));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CoSo = coSo;
                return View(model);
            }

            try
            {
                var client = CreateHttpClientWithAuth();
                var response = await client.PutAsJsonAsync($"api/suaphonghoc/{id}", model);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Cập nhật phòng học thành công!";
                    return RedirectToAction(nameof(FacilityDetails), new { id = model.CoSoId });
                }

                var errMsg = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Lỗi API khi cập nhật phòng học: {errMsg}";
                ViewBag.CoSo = coSo;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                ViewBag.CoSo = coSo;
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("xoaphonghoc/{id}")]

        public async Task<IActionResult> DeleteRoom(int id)
        {
            var phongHoc = await _context.PhongHocs.FindAsync(id);
            if (phongHoc == null)
            {
                return NotFound(new { result = false, message = "Không tìm thấy phòng học." });
            }

            var inUse = await _context.LichHocs.AnyAsync(lh => lh.PhongHocId == id);
            if (inUse)
            {
                return BadRequest(new { result = false, message = "Phòng học đang được sử dụng trong lịch học." });
            }
            try
            {
                _context.PhongHocs.Remove(phongHoc);
                await _context.SaveChangesAsync();
                return Ok(new { result = true, message = "Xóa phòng học thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { result = false, message = "Lỗi hệ thống: " + ex.Message });
            }

        }

        //Trang danh sách đơn phiếu giảng viên
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DanhSachDonPhieu(string maLop, string trangThai, string ngayGui)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://localhost:5001/");
                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("api/danhsachdonphieu");
                List<XinVangDayViewModel> danhSachDon = new();

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(body);
                    danhSachDon = JsonConvert.DeserializeObject<List<XinVangDayViewModel>>(result.data.ToString());
                }

                // Filter tại server side
                if (!string.IsNullOrEmpty(maLop))
                    danhSachDon = danhSachDon.Where(x => x.MaLopHocPhan != null && x.MaLopHocPhan.Contains(maLop)).ToList();

                if (!string.IsNullOrEmpty(trangThai))
                    danhSachDon = danhSachDon.Where(x => x.TrangThai == trangThai).ToList();

                if (!string.IsNullOrEmpty(ngayGui))
                {
                    if (DateTime.TryParse(ngayGui, out var dt))
                        danhSachDon = danhSachDon.Where(x => x.CreatedAt.Date == dt.Date).ToList();
                }

                return View(danhSachDon);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi: " + ex.Message;
                return View(new List<XinVangDayViewModel>());
            }
        }

    }
}