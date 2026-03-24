using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using AiLaTrieuPhu.Data;
using System.Linq;
using System;

namespace AiLaTrieuPhu.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 1. Lấy thông tin tài khoản đang đăng nhập từ Database
            // Cần lấy trực tiếp từ DB để đảm bảo trường Email luôn mới nhất
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null) return RedirectToAction("Logout", "Account");

            // 2. Lấy toàn bộ lịch sử chơi của tài khoản này
            var userGames = _context.Games.Where(g => g.UserId == userId).ToList();

            // 3. Thực hiện tính toán thống kê (Giữ nguyên logic cũ)
            int totalGames = userGames.Count;
            int totalWins = userGames.Count(g => g.LevelReached >= 15);
            int totalLosses = totalGames - totalWins;

            double averageLevel = totalGames > 0 ? userGames.Average(g => (double)g.LevelReached) : 0;
            long totalMoney = totalGames > 0 ? userGames.Sum(g => g.Money) : 0;

            // 4. Bơm dữ liệu sang View (Giao diện)
            ViewBag.UserId = userId;
            ViewBag.Username = user.Username;
            ViewBag.Role = user.Role; // Lấy từ DB cho chắc chắn

            // ================= PHẦN CẬP NHẬT MỚI =================
            // Truyền Email ra để View kiểm tra và hiện Popup
            ViewBag.UserEmail = user.Email;
            // =====================================================

            ViewBag.TotalGames = totalGames;
            ViewBag.TotalWins = totalWins;
            ViewBag.TotalLosses = totalLosses;
            ViewBag.AverageLevel = Math.Round(averageLevel, 1);
            ViewBag.TotalMoney = totalMoney;

            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}