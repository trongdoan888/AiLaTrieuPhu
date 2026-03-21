using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace AiLaTrieuPhu.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Lấy ID người dùng từ Session
            var userId = HttpContext.Session.GetInt32("UserId");

            // Nếu chưa đăng nhập, đá về trang Login
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Truyền ID sang View thông qua ViewBag cực kỳ an toàn
            ViewBag.UserId = userId;

            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}