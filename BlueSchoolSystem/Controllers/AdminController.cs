using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Diagnostics;
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
        private readonly IConfiguration configuration;
        private readonly string? _apiBaseUrl;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ILogger<AdminController> logger, IHttpClientFactory httpClientFactory, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _userManager = userManager;
            this.configuration = configuration;

            _apiBaseUrl = configuration["ApiSettings:BaseUrl"];

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

    }
}
