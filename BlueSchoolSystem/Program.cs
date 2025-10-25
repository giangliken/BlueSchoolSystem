using BlueSchoolSystem;
using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using BlueSchoolSystem.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using System.Text;
using System.Threading.RateLimiting;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

ExcelPackage.License.SetNonCommercialOrganization("BlueSchoolSystem - NonCommercial Org");


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.

builder.Services.AddScoped<IActivityLogService, EFActivityLogService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();


// 1. Đăng ký dịch vụ localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 2. Cấu hình ngôn ngữ mặc định
builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    var supportedCultures = new[] { "vi-VN", "en-US" };
    opts.SetDefaultCulture("vi-VN")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

// 3. Đăng ký Razor Pages, bật DataAnnotations localization
builder.Services.AddRazorPages()
       .AddDataAnnotationsLocalization();



builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

});


builder.Services.Configure<GoogleAuthSettings>(
    builder.Configuration.GetSection("Authentication:Google"));


// Cấu hình JwtSettings
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);
var jwtSettings = jwtSettingsSection.Get<JwtSettings>();


// Cấu hình JWT Auth
var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

// Add Google authentication
builder.Services.AddAuthentication()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.SaveTokens = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.ClaimActions.MapJsonKey("picture", "picture");
    options.ClaimActions.MapJsonKey("locale", "locale");
})
.AddJwtBearer(options =>
 {
     options.TokenValidationParameters = new TokenValidationParameters
     {
         ValidateIssuer = true,
         ValidateAudience = true,
         ValidateLifetime = true,
         ValidateIssuerSigningKey = true,
         ValidIssuer = jwtSettings.Issuer,
         ValidAudience = jwtSettings.Audience,
         IssuerSigningKey = new SymmetricSecurityKey(key),
         ClockSkew = TimeSpan.Zero
     };
 });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        // Nếu là request API thì trả về 401 thay vì redirect
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["Accept"].ToString().Contains("application/json") ||
            context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        // Nếu không phải API, cho redirect như bình thường
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginLimiter", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.RejectionStatusCode = 429;

});


builder.Services.AddSession();
builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
.AddDefaultTokenProviders()
.AddDefaultUI()
.AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddRazorPages();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IFaceTicketStore, EFMemoryFaceTicketStore>();
builder.Services.Configure<FaceVerifyOptions>(builder.Configuration.GetSection("FaceVerify"));


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await Seeder.SeedAsync(services); // Gọi hàm seed
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

var loc = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(loc.Value);

app.UseSession();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
