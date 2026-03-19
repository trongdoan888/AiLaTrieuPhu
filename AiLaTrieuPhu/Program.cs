using AiLaTrieuPhu.Data;
using Microsoft.EntityFrameworkCore;
using AiLaTrieuPhu.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!db.Questions.Any())
    {
        db.Questions.AddRange(
            new Question { Content = "2+2=?", A = "3", B = "4", C = "5", D = "6", CorrectAnswer = "B", Level = 1 },
            new Question { Content = "Thủ đô VN?", A = "HCM", B = "Huế", C = "Hà Nội", D = "Đà Nẵng", CorrectAnswer = "C", Level = 1 }
        );

        db.SaveChanges();
    }
}
app.Run();