using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Bắt buộc để dùng Session

namespace AiLaTrieuPhu.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Kiểm tra nếu chưa đăng nhập thì đẩy về trang Login
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // Thêm hàm này để hiển thị trang Luật chơi / Giới thiệu
        public IActionResult About()
        {
            return View();
        }
    }
}