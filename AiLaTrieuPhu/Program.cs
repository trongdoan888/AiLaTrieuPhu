using AiLaTrieuPhu.Data;
using Microsoft.EntityFrameworkCore;
using AiLaTrieuPhu.Hubs; // Thêm thư viện để nhận diện GameHub

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ MVC
builder.Services.AddControllersWithViews();

// Kết nối PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm Session để lưu trạng thái người chơi
builder.Services.AddSession();

// Kích hoạt SignalR cho tính năng đấu 2 người
builder.Services.AddSignalR();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Cấu hình định tuyến mặc định vào trang Đăng nhập
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Map đường dẫn cho SignalR Hub (Cổng kết nối Socket)
app.MapHub<GameHub>("/gamehub");

app.Run();