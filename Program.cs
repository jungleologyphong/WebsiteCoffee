using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebsiteCoffeeShop.Config;
using WebsiteCoffeeShop.Context;
using WebsiteCoffeeShop.IRepository;
using WebsiteCoffeeShop.Models;
using WebsiteCoffeeShop.Repositories;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình database
// Cấu hình database dùng MySQL (qua XAMPP)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));


// cấu hình VN PAY
builder.Services.Configure<VNPaySettings>(builder.Configuration.GetSection("VNPay"));

// Cấu hình Identity (chỉ giữ AddIdentity, không dùng AddDefaultIdentity)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = false; // Không yêu cầu xác nhận email trước khi đăng nhập
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Cấu hình session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình đường dẫn đăng nhập, đăng xuất
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Đăng ký các services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Đảm bảo Razor Pages hoạt động cho Identity
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();

// thêm pdf 
//builder.Services.AddSingleton(typeof(IConverter), CustomConverter.Instance);

QuestPDF.Settings.License = LicenseType.Community;

// Tạo ứng dụng web
var app = builder.Build();

// Middleware xử lý lỗi
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Kích hoạt session
app.UseSession();

// Middleware cơ bản
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Định tuyến trang Razor (cho Identity hoạt động)
app.MapRazorPages();
// Định tuyến khu vực Admin
app.MapAreaControllerRoute(
    name: "Admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Statistics}/{action=Index}/{id?}");


// Định tuyến khu vực Employee
app.MapAreaControllerRoute(
    name: "Employee",
    areaName: "Employee",
    pattern: "Employee/{controller=Product}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
