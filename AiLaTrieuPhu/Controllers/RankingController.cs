using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AiLaTrieuPhu.Controllers
{
    // Class phụ trợ để chứa dữ liệu bảng Tổng Tiền
    public class UserMoneyRanking
    {
        public string Username { get; set; }
        public long TotalMoney { get; set; }
    }

    public class RankingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RankingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // BẢNG 1: Điểm cao nhất & Thời gian nhanh nhất (Lấy từ bảng Rankings)
            var topScores = _context.Rankings
                .Include(r => r.User)
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.TimePlayed)
                .ToList();

            // BẢNG 2: Tổng tiền tích lũy (Tính tổng Money từ các ván chơi trong bảng Games)
            var topMoney = _context.Users
                .Select(u => new UserMoneyRanking
                {
                    Username = u.Username,
                    TotalMoney = u.Games.Sum(g => g.Money)
                })
                .Where(u => u.TotalMoney > 0) // Chỉ hiển thị những người có cày ra tiền
                .OrderByDescending(u => u.TotalMoney)
                .ToList();

            // Truyền 2 danh sách sang Giao diện qua ViewBag
            ViewBag.TopScores = topScores;
            ViewBag.TopMoney = topMoney;

            // Chú ý: Ta không cần truyền model trực tiếp nữa, dùng ViewBag là đủ.
            return View();
        }
    }
}