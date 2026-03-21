using AiLaTrieuPhu.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();

// Kết nối PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm Session để lưu trạng thái người chơi
builder.Services.AddSession();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Cấu hình định tuyến mặc định vào trang Đăng nhập
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();