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

        // Bắt buộc phải có constructor này để kết nối Database
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

            // 1. Lấy thông tin tài khoản đang đăng nhập
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            // 2. Lấy toàn bộ lịch sử chơi của tài khoản này
            var userGames = _context.Games.Where(g => g.UserId == userId).ToList();

            // 3. Thực hiện tính toán thống kê
            int totalGames = userGames.Count;
            int totalWins = userGames.Count(g => g.LevelReached >= 15); // Vượt 15 câu là Thắng
            int totalLosses = totalGames - totalWins; // Còn lại là Thua

            // Tính trung bình level (ép kiểu double để chia lấy phần thập phân)
            double averageLevel = totalGames > 0 ? userGames.Average(g => (double)g.LevelReached) : 0;

            // Tính tổng tiền
            long totalMoney = totalGames > 0 ? userGames.Sum(g => g.Money) : 0;

            // 4. Bơm dữ liệu sang View (Giao diện) thông qua ViewBag
            ViewBag.UserId = userId;
            ViewBag.Username = user?.Username ?? "Khách";
            ViewBag.Role = HttpContext.Session.GetString("Role");

            ViewBag.TotalGames = totalGames;
            ViewBag.TotalWins = totalWins;
            ViewBag.TotalLosses = totalLosses;
            ViewBag.AverageLevel = Math.Round(averageLevel, 1); // Làm tròn 1 chữ số
            ViewBag.TotalMoney = totalMoney;

            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}