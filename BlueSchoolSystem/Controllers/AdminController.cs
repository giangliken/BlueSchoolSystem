using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
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
        private readonly IConfiguration configuration;
        private readonly string? _apiBaseUrl;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityLogService _activityLogService;

        public AdminController(ILogger<AdminController> logger, IHttpClientFactory httpClientFactory, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration, IActivityLogService activityLogService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _userManager = userManager;
            this.configuration = configuration;

            _apiBaseUrl = configuration["ApiSettings:BaseUrl"];
            _activityLogService = activityLogService;

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
                .FirstOrDefaultAsync(g => g.MaGiangVien == maGiangVien);

            if (giangVien == null)
            {
                TempData["Error"] = $"Không tìm thấy giảng viên với mã: {maGiangVien}";
                return RedirectToAction("TeacherManager");
            }

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
        public async Task<IActionResult> SemesterManager()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            // Lấy token
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Lấy danh sách trạng thái để đổ vào dropdown filter
            var trangThaiList = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "HocKy")
                .ToListAsync();
            ViewBag.TrangThaiList = trangThaiList;

            HttpResponseMessage response;
            try
            {
                // Gọi API lấy danh sách học kỳ
                response = await client.GetAsync("api/hocky");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API lấy danh sách học kỳ.");
                TempData["Error"] = "Không thể kết nối đến hệ thống API.";
                return View(new List<HocKy>());
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể lấy danh sách học kỳ từ hệ thống API.";
                return View(new List<HocKy>());
            }

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var dataElement))
            {
                TempData["Error"] = "Dữ liệu học kỳ không hợp lệ.";
                return View(new List<HocKy>());
            }

            // Sử dụng JsonSerializer để deserialize danh sách Học Kỳ
            var hockys = System.Text.Json.JsonSerializer.Deserialize<List<HocKy>>(dataElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(hockys ?? new List<HocKy>());
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
        //Quản lý đợt đăng ký
        public async Task<IActionResult> RegistrationPeriods()
        {
            // Lấy danh sách Học kỳ từ API (để dropdown chọn)
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiBaseUrl ?? "https://localhost:5001/");
                var token = HttpContext.Session.GetString("access_token");
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var resp = await client.GetAsync("api/hocky");
                if (!resp.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không thể lấy danh sách học kỳ từ API.";
                    ViewBag.HocKyList = new SelectList(new List<HocKy>(), "Id", "TenHocKy");
                    return View();
                }

                var body = await resp.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataEl))
                {
                    var hockys = System.Text.Json.JsonSerializer.Deserialize<List<HocKy>>(dataEl.GetRawText(),
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<HocKy>();

                    // Lọc theo trạng thái
                    var filteredHockys = hockys
                        .Where(h => h.TrangThai != null &&
                                    (h.TrangThai.TenTrangThai == "Hoạt động" || h.TrangThai.TenTrangThai == "Mới tạo"))
                        .ToList();

                    filteredHockys.Insert(0, new HocKy { Id = 0, TenHocKy = "Tất cả" });
                    ViewBag.HocKyList = new SelectList(filteredHockys, "Id", "TenHocKy");
                }
                else
                {
                    ViewBag.HocKyList = new SelectList(new List<HocKy>(), "Id", "TenHocKy");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API lấy Học kỳ");
                TempData["Error"] = "Không thể kết nối đến hệ thống API.";
                ViewBag.HocKyList = new SelectList(new List<HocKy>(), "Id", "TenHocKy");
            }


            // Load tất cả đợt đăng ký **kèm HocKy**
            var periods = await _context.DotDangKys
                .Include(d => d.HocKy)  // <- Thêm Include này
                .ToListAsync();

            return View(periods ?? new List<DotDangKy>());
        }

        // --------------------------
        // Partial table (AJAX)
        // --------------------------
        // GET: /Admin/GetRegistrationPeriodsTable?hocKyId=1
        public async Task<IActionResult> GetRegistrationPeriodsTable(int hocKyId)
        {
            List<DotDangKy> list;
            if (hocKyId == 0)
            {
                list = await _context.DotDangKys
                    .Include(d => d.HocKy)
                    .OrderBy(d => d.NgayBatDau)
                    .ToListAsync();
            }
            else
            {
                list = await _context.DotDangKys
                    .Where(d => d.HocKyId == hocKyId)
                    .Include(d => d.HocKy)
                    .OrderBy(d => d.NgayBatDau)
                    .ToListAsync();
            }

            ViewBag.HocKyId = hocKyId;
            return PartialView("_RegistrationPeriodsTable", list);
        }

        // --------------------------
        // Create
        // --------------------------
        // GET: /Admin/CreateRegistrationPeriod?hocKyId=X
        public async Task<IActionResult> CreateRegistrationPeriod(int? hocKyId)
        {
            if (!hocKyId.HasValue || hocKyId.Value == 0)
            {
                TempData["Error"] = "Vui lòng chọn Học kỳ trước khi tạo Đợt Đăng ký.";
                return RedirectToAction(nameof(RegistrationPeriods));
            }

            var hocky = await _context.HocKys.FindAsync(hocKyId.Value);
            if (hocky == null)
            {
                TempData["Error"] = "Học kỳ không tồn tại.";
                return RedirectToAction(nameof(RegistrationPeriods));
            }

            await LoadViewBags(hocKyId.Value);

            var defaultDto = new DotDangKyDTO
            {
                HocKyId = hocKyId.Value,
                NgayBatDau = DateTime.Today.AddDays(1),
                NgayKetThuc = DateTime.Today.AddDays(7),
                LoaiThaoTac = "Đăng ký mới"
            };

            return View(defaultDto);
        }

        // POST: /Admin/CreateRegistrationPeriod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRegistrationPeriod(DotDangKyDTO dto)
        {
            await LoadViewBags(dto.HocKyId);

            if (dto == null)
            {
                ModelState.AddModelError("", "Dữ liệu không hợp lệ.");
                return View(dto);
            }

            if (dto.NgayBatDau >= dto.NgayKetThuc)
            {
                ModelState.AddModelError(nameof(dto.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (dto.DoiTuongApDungs == null || !dto.DoiTuongApDungs.Any())
            {
                ModelState.AddModelError("", "Phải chọn ít nhất một đối tượng áp dụng.");
            }
            if (dto.HocKyId == 0 || !await _context.HocKys.AnyAsync(h => h.Id == dto.HocKyId))
            {
                ModelState.AddModelError(nameof(dto.HocKyId), "Học kỳ không hợp lệ.");
                await LoadViewBags(dto.HocKyId);
                return View(dto);
            }
            if (!ModelState.IsValid) return View(dto);

            var newDots = new List<DotDangKy>();
            foreach (var obj in dto.DoiTuongApDungs)
            {
                var entity = new DotDangKy
                {
                    HocKyId = dto.HocKyId,
                    TenDot = dto.TenDot,
                    NgayBatDau = dto.NgayBatDau,
                    NgayKetThuc = dto.NgayKetThuc,
                    LoaiThaoTac = dto.LoaiThaoTac,
                    LoaiDoiTuong = obj.Loai,
                    GiaTriDoiTuong = obj.GiaTri,
                    IsActive = true
                };
                newDots.Add(entity);
            }

            await _context.DotDangKys.AddRangeAsync(newDots);
            await _context.SaveChangesAsync();

            // Log
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId")?.Value;
                var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
                if (!string.IsNullOrEmpty(userId))
                {
                    await _activityLogService.LogAsync(
                        userId: userId,
                        userName: userName ?? "Unknown",
                        device: Request.Headers["User-Agent"].ToString(),
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                        actionType: "CREATE",
                        tableName: "DotDangKys",
                        objectId: newDots.FirstOrDefault()?.Id.ToString() ?? "0",
                        description: $"Tạo đợt đăng ký '{dto.TenDot}' cho HK {dto.HocKyId}. Số đối tượng: {dto.DoiTuongApDungs.Count}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ghi log thất bại sau khi tạo DotDangKy");
            }

            return RedirectToAction("RegistrationPeriods", new { hocKyId = dto.HocKyId });
        }

        // --------------------------
        // Edit (group edit: sửa toàn bộ đợt dựa theo TenDot + LoaiThaoTac + HocKyId)
        // --------------------------
        // GET: /Admin/EditRegistrationPeriod/{id}
        public async Task<IActionResult> EditRegistrationPeriod(int id)
        {
            var first = await _context.DotDangKys.FindAsync(id);
            if (first == null)
            {
                TempData["Error"] = "Không tìm thấy đợt đăng ký.";
                return RedirectToAction(nameof(RegistrationPeriods));
            }

            var group = await _context.DotDangKys
                .Where(d => d.HocKyId == first.HocKyId &&
                            d.TenDot == first.TenDot &&
                            d.LoaiThaoTac == first.LoaiThaoTac)
                .ToListAsync();

            var dto = new DotDangKyDTO
            {
                HocKyId = first.HocKyId,
                TenDot = first.TenDot,
                NgayBatDau = first.NgayBatDau,
                NgayKetThuc = first.NgayKetThuc,
                LoaiThaoTac = first.LoaiThaoTac,
                DoiTuongApDungs = group
                    .Select(g => new DoiTuongApDungDTO
                    {
                        Loai = g.LoaiDoiTuong,
                        GiaTri = g.GiaTriDoiTuong
                    }).ToList()
            };

            // Nếu không có thì thêm 1 đối tượng mặc định
            if (!dto.DoiTuongApDungs.Any())
            {
                dto.DoiTuongApDungs.Add(new DoiTuongApDungDTO
                {
                    Loai = "NIEN_KHOA",
                    GiaTri = ""
                });
            }

            await LoadViewBags(first.HocKyId);
            return View(dto);
        }

        // POST: /Admin/EditRegistrationPeriod/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRegistrationPeriod(int id, DotDangKyDTO dto)
        {
            if (dto == null)
            {
                ModelState.AddModelError("", "Dữ liệu không hợp lệ.");
                await LoadViewBags(dto?.HocKyId ?? 0);
                return View(dto);
            }

            if (dto.NgayBatDau >= dto.NgayKetThuc)
            {
                ModelState.AddModelError(nameof(dto.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (dto.DoiTuongApDungs == null || !dto.DoiTuongApDungs.Any())
            {
                ModelState.AddModelError("", "Phải có ít nhất một đối tượng áp dụng.");
            }

            if (!ModelState.IsValid)
            {
                await LoadViewBags(dto.HocKyId);
                return View(dto);
            }

            var existing = await _context.DotDangKys.FindAsync(id);
            if (existing == null)
            {
                ModelState.AddModelError("", "Không tìm thấy đợt để cập nhật.");
                await LoadViewBags(dto.HocKyId);
                return View(dto);
            }

            // Xóa toàn bộ nhóm cũ
            var groupToRemove = await _context.DotDangKys
                .Where(d => d.HocKyId == existing.HocKyId &&
                            d.TenDot == existing.TenDot &&
                            d.LoaiThaoTac == existing.LoaiThaoTac)
                .ToListAsync();

            _context.DotDangKys.RemoveRange(groupToRemove);

            // Tạo nhóm mới
            var newDots = dto.DoiTuongApDungs.Select(obj => new DotDangKy
            {
                HocKyId = dto.HocKyId,
                TenDot = dto.TenDot,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc,
                LoaiThaoTac = dto.LoaiThaoTac,
                LoaiDoiTuong = obj.Loai,
                GiaTriDoiTuong = obj.GiaTri,
                IsActive = true
            }).ToList();

            await _context.DotDangKys.AddRangeAsync(newDots);
            await _context.SaveChangesAsync();

            return RedirectToAction("RegistrationPeriods", new { hocKyId = dto.HocKyId });
        }


        // --------------------------
        // Delete (xóa toàn bộ nhóm cùng TenDot + LoaiThaoTac + HocKyId)
        // --------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRegistrationPeriod(int id)
        {
            var first = await _context.DotDangKys.FindAsync(id);
            if (first == null)
            {
                TempData["Error"] = "Không tìm thấy đợt đăng ký.";
                return RedirectToAction(nameof(RegistrationPeriods));
            }

            var group = await _context.DotDangKys
                .Where(d => d.HocKyId == first.HocKyId && d.TenDot == first.TenDot && d.LoaiThaoTac == first.LoaiThaoTac)
                .ToListAsync();

            _context.DotDangKys.RemoveRange(group);
            await _context.SaveChangesAsync();

            // Log
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId")?.Value;
                var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
                if (!string.IsNullOrEmpty(userId))
                {
                    await _activityLogService.LogAsync(
                        userId: userId,
                        userName: userName ?? "Unknown",
                        device: Request.Headers["User-Agent"].ToString(),
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                        actionType: "DELETE",
                        tableName: "DotDangKys",
                        objectId: $"{first.HocKyId}_{first.TenDot}_{first.LoaiThaoTac}",
                        description: $"Xóa đợt đăng ký '{first.TenDot}' cho HK {first.HocKyId}. Xóa {group.Count} bản ghi."
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ghi log thất bại sau khi xóa DotDangKy");
            }

            return RedirectToAction("RegistrationPeriods", new { hocKyId = first.HocKyId });
        }

        // --------------------------
        // Helpers
        // --------------------------
        private async Task LoadViewBags(int hocKyId)
        {
            // Lấy học kỳ hiện tại
            ViewBag.HocKy = await _context.HocKys.FindAsync(hocKyId);

            // Bổ sung danh sách tất cả học kỳ cho dropdown
            var hockys = await _context.HocKys
                .Include(h => h.TrangThai)
                .Where(h => h.TrangThai.TenTrangThai == "Hoạt động" || h.TrangThai.TenTrangThai == "Mới tạo")
                .OrderByDescending(h => h.NgayBatDau)
                .ToListAsync();
            ViewBag.HocKyList = new SelectList(hockys, "Id", "TenHocKy", hocKyId);


            // Các ViewBag khác
            ViewBag.NienKhoaList = await _context.SinhViens
                .Select(s => s.NgayNhapHoc.Year.ToString())
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();

            ViewBag.KhoaList = await _context.Khoas.ToListAsync();

            ViewBag.ThaoTacList = new List<string>
    {
        "DANGKY",
        "HUY",
        "RUT",
        "DIEUCHINH",
        "DANGKY_SOM"
    };

            ViewBag.LoaiDoiTuongList = new List<string>
    {
        "NIEN_KHOA",
        "KHOA",
        "NGANH",
        "LOAIHINH"
    };
        }

        // Quản lý lớp học phần

        // GET: /Admin/CourseClassManager
        public async Task<IActionResult> CourseClassManager()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            HttpResponseMessage response;
            try
            {
                // Gọi API lấy danh sách LHP
                response = await client.GetAsync("api/lophocphans");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API lấy danh sách Lớp Học Phần.");
                TempData["Error"] = "Không thể kết nối đến hệ thống API.";
                return View(new List<object>()); // Trả về list rỗng nếu lỗi
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể lấy danh sách Lớp Học Phần từ hệ thống API.";
                return View(new List<object>());
            }

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var dataElement = root.GetProperty("data");

            // Dùng dynamic hoặc ViewModel nếu bạn có định nghĩa
            // Trong ví dụ này, tôi dùng dynamic để đơn giản hóa việc parse object phức tạp
            var lhpList = JsonConvert.DeserializeObject<List<dynamic>>(dataElement.GetRawText());

            // Lấy danh sách trạng thái để đổ vào modal cập nhật
            ViewBag.TrangThaiLHPList = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "LopHocPhan")
                .ToListAsync();
            // Lấy danh sách trạng thái
            ViewBag.TrangThaiLHPList = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "LopHocPhan")
                .ToListAsync();

            // 🔹 Nạp danh sách học kỳ cho modal Auto đăng ký
            var hocKyList = await _context.HocKys.ToListAsync();
            ViewBag.HocKyList = hocKyList;
            ViewBag.HocKyId = hocKyList.FirstOrDefault()?.Id ?? 0;
            return View(lhpList);
        }

        // GET: /Admin/CreateCourseClass
        public async Task<IActionResult> CreateCourseClass()
        {
            // Chuẩn bị SelectList cho form tạo LHP
            ViewBag.HocKyList = new SelectList(await _context.HocKys.ToListAsync(), "Id", "TenHocKy");
            ViewBag.MonHocList = new SelectList(await _context.MonHocs.ToListAsync(), "Id", "TenMonHoc");
            ViewBag.GiangVienList = new SelectList(
                await _context.GiangViens.Select(gv => new { gv.Id, HoTen = gv.HoVaTenDem + " " + gv.Ten }).ToListAsync(),
                "Id", "HoTen");

            var defaultDto = new LopHocPhanCreateDTO
            {
                NgayBatDauLHP = DateTime.Today,
                NgayKetThucLHP = DateTime.Today.AddMonths(3),
            };

            return View(defaultDto);
        }

        // POST: /Admin/CreateCourseClass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseClass(LopHocPhanCreateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                // Re-populate ViewBags
                ViewBag.HocKyList = new SelectList(await _context.HocKys.ToListAsync(), "Id", "TenHocKy", dto.HocKyId);
                ViewBag.MonHocList = new SelectList(await _context.MonHocs.ToListAsync(), "Id", "TenMonHoc", dto.MonHocId);
                ViewBag.GiangVienList = new SelectList(
                    await _context.GiangViens.Select(gv => new { gv.Id, HoTen = gv.HoVaTenDem + " " + gv.Ten }).ToListAsync(),
                    "Id", "HoTen", dto.GiangVienId);
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
                var json = JsonConvert.SerializeObject(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gọi API tạo LHP
                var response = await client.PostAsync("api/lophocphan", content);

                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadAsStringAsync();
                    dynamic resultObj = JsonConvert.DeserializeObject(apiResult);
                    int newLhpId = resultObj.lopHocPhanId;

                    TempData["Success"] = $"Tạo Lớp Học Phần **{dto.MaLopHocPhan}** thành công! Vui lòng thêm lịch học.";
                    // Redirect đến trang thêm lịch học (tạm thời redirect về Manager)
                    // Tốt hơn nên redirect đến: /Admin/AddSchedule?lhpId={newLhpId}
                    return RedirectToAction(nameof(CourseClassManager));
                }
                else
                {
                    var apiError = await response.Content.ReadAsStringAsync();
                    dynamic errObj = JsonConvert.DeserializeObject(apiError);
                    ModelState.AddModelError("", errObj?.message ?? "Lỗi tạo Lớp Học Phần từ API.");

                    // Re-populate ViewBags
                    ViewBag.HocKyList = new SelectList(await _context.HocKys.ToListAsync(), "Id", "TenHocKy", dto.HocKyId);
                    ViewBag.MonHocList = new SelectList(await _context.MonHocs.ToListAsync(), "Id", "TenMonHoc", dto.MonHocId);
                    ViewBag.GiangVienList = new SelectList(
                        await _context.GiangViens.Select(gv => new { gv.Id, HoTen = gv.HoVaTenDem + " " + gv.Ten }).ToListAsync(),
                        "Id", "HoTen", dto.GiangVienId);
                    return View(dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối khi tạo Lớp Học Phần.");
                ModelState.AddModelError("", $"Lỗi kết nối: {ex.Message}");
                // Re-populate ViewBags
                ViewBag.HocKyList = new SelectList(await _context.HocKys.ToListAsync(), "Id", "TenHocKy", dto.HocKyId);
                ViewBag.MonHocList = new SelectList(await _context.MonHocs.ToListAsync(), "Id", "TenMonHoc", dto.MonHocId);
                ViewBag.GiangVienList = new SelectList(
                    await _context.GiangViens.Select(gv => new { gv.Id, HoTen = gv.HoVaTenDem + " " + gv.Ten }).ToListAsync(),
                    "Id", "HoTen", dto.GiangVienId);
                return View(dto);
            }
        }

        // GET: /Admin/AddSchedule?lhpId=X (Thêm lịch học cho LHP đã tạo)
        public async Task<IActionResult> AddSchedule(int lhpId)
        {
            var lhp = await _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.GiangVien)
                .FirstOrDefaultAsync(l => l.Id == lhpId);

            if (lhp == null)
            {
                TempData["Error"] = "Không tìm thấy Lớp Học Phần.";
                return RedirectToAction(nameof(CourseClassManager));
            }

            ViewBag.LHP = lhp;
            ViewBag.PhongHocList = new SelectList(
                _context.PhongHocs.ToList(),
                "Id", "MaPhongHoc"
            );

            var model = new LichHocDTO
            {
                LopHocPhanId = lhp.Id,
                Ngay = DateTime.Today,
                GioBatDau = TimeSpan.FromHours(8),
                GioKetThuc = TimeSpan.FromHours(10)
            };

            return View(model); // ✅ luôn trả về model không null
        }




        // POST: /Admin/AddSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(LichHocDTO dto)
        {
            // Lấy LHP và các thuộc tính liên quan
            var lhp = await _context.LopHocPhans
                .Include(l => l.GiangVien)
                .Include(l => l.MonHoc)
                .FirstOrDefaultAsync(l => l.Id == dto.LopHocPhanId);

            // Logic chung để nạp lại ViewBag trong trường hợp lỗi (được đặt trước return View)
            Func<IActionResult> RePopulateViewAndReturn = () =>
            {
                ViewBag.LHP = lhp;
                // Luôn đảm bảo PhongHocList được nạp
                ViewBag.PhongHocList = new SelectList(
                    _context.PhongHocs.ToList(),
                    "Id", "MaPhong",
                    dto.PhongHocId
                );

                // Kiểm tra an toàn cho GiangVien
                if (lhp != null && lhp.GiangVien != null)
                {
                    ViewBag.GiangVien = $"{lhp.GiangVien.HoVaTenDem} {lhp.GiangVien.Ten} (Mã: {lhp.GiangVien.MaGiangVien})";
                }
                else
                {
                    ViewBag.GiangVien = "Không rõ (Lỗi dữ liệu giảng viên)";
                }

                // Nếu LHP bị mất sau khi POST, ta không thể tiếp tục
                if (lhp == null)
                {
                    TempData["Error"] = "Lỗi nghiêm trọng: Không tìm thấy Lớp Học Phần khi xử lý lịch học.";
                    return RedirectToAction(nameof(CourseClassManager));
                }

                return View(dto);
            };

            if (!ModelState.IsValid)
            {
                return RePopulateViewAndReturn();
            }

            // Kiểm tra lhp có null không trước khi tiếp tục (trường hợp LHP bị xóa giữa chừng)
            if (lhp == null)
            {
                TempData["Error"] = "Lỗi: Không tìm thấy Lớp Học Phần để thêm lịch học.";
                return RedirectToAction(nameof(CourseClassManager));
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
                var json = JsonConvert.SerializeObject(dto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gọi API tạo Lịch Học
                var response = await client.PostAsync("api/lophocphan/lichhoc", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = $"Thêm lịch học thành công cho LHP ID {dto.LopHocPhanId}!";
                    return RedirectToAction(nameof(CourseClassManager));
                }
                else
                {
                    var apiError = await response.Content.ReadAsStringAsync();
                    dynamic errObj = JsonConvert.DeserializeObject(apiError);
                    ModelState.AddModelError("", errObj?.message ?? "Lỗi thêm lịch học từ API.");

                    // Re-populate ViewBags
                    return RePopulateViewAndReturn();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối khi thêm lịch học.");
                ModelState.AddModelError("", $"Lỗi kết nối: {ex.Message}");

                // Re-populate ViewBags
                return RePopulateViewAndReturn();
            }
        }


        // POST: /Admin/UpdateCourseClassStatus
        // Thường gọi qua Ajax từ trang CourseClassManager
        [HttpPost]
        public async Task<IActionResult> UpdateCourseClassStatus(int lopHocPhanId, int trangThaiId, string lyDo = null)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("access_token");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var dto = new UpdateLopHocPhanStatusDTO { LopHocPhanId = lopHocPhanId, TrangThaiId = trangThaiId, LyDo = lyDo };
            var json = JsonConvert.SerializeObject(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PutAsync("api/lophocphan/trangthai", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    dynamic successObj = JsonConvert.DeserializeObject(body);
                    return Json(new { success = true, message = successObj.message });
                }
                else
                {
                    var apiError = await response.Content.ReadAsStringAsync();


                    _logger.LogWarning("API UpdateStatus failed. StatusCode: {StatusCode}. Error: {Error}", response.StatusCode, apiError);

                    dynamic errObj = JsonConvert.DeserializeObject(apiError);
                    return Json(new { success = false, message = errObj?.message ?? "Cập nhật trạng thái không thành công." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối khi cập nhật trạng thái LHP.");
                return Json(new { success = false, message = $"Lỗi kết nối: {ex.Message}" });
            }
        }
        [HttpPost]
        public IActionResult DeleteCourseClass(int lopHocPhanId)
        {
            try
            {
                var lhp = _context.LopHocPhans.Find(lopHocPhanId);
                if (lhp == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy lớp học phần." });
                }

                _context.LopHocPhans.Remove(lhp);
                _context.SaveChanges();

                return Json(new { success = true, message = "Xóa lớp học phần thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        //chức năng tự động đăng kí lhp
        [HttpPost]
        public async Task<IActionResult> RunAutoDangKyBatBuocFromAdmin(int hocKyId, int thuTuHocKy)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("access_token");

            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.PostAsync(
                    $"api/auto-dangky-batbuoc/{hocKyId}?thuTuHocKy={thuTuHocKy}",
                    null
                );

                var body = await response.Content.ReadAsStringAsync();
                dynamic resultObj = JsonConvert.DeserializeObject(body);

                if (response.IsSuccessStatusCode && resultObj.result == true)
                {
                    TempData["Success"] = resultObj.message.ToString();
                }
                else
                {
                    TempData["Error"] = resultObj?.message ?? "Chạy Auto đăng ký thất bại.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi kết nối API: {ex.Message}";
            }

            return RedirectToAction(nameof(CourseClassManager));
        }

    }
}
