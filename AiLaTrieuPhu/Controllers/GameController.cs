using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace AiLaTrieuPhu.Controllers
{
    public class GameController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Các mốc tiền thưởng (Level 1 đến 15)
        private readonly int[] moneyMilestones = { 0, 200, 400, 600, 1000, 2000, 3000, 6000, 10000, 14000, 22000, 30000, 40000, 60000, 85000, 150000 };

        public GameController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. Kiểm tra đăng nhập chặt chẽ
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int currentLevel = HttpContext.Session.GetInt32("CurrentLevel") ?? 1;

            if (currentLevel == 1)
            {
                HttpContext.Session.SetInt32("Score", 0);
                HttpContext.Session.SetInt32("TotalTime", 0);
                HttpContext.Session.SetInt32("Money", 0);
            }

            // 2. FIX LỖI "OUT GAME KHI ĐÚNG" (Chia độ khó DB)
            // Từ câu 1 đến 5 bốc Level 1. Từ câu 6 đến 15 bốc Level 2.
            int dbDifficulty = currentLevel <= 5 ? 1 : 2;

            var question = _context.Questions
                .Where(q => q.Level == dbDifficulty)
                .OrderBy(q => Guid.NewGuid())
                .FirstOrDefault();

            if (question == null)
            {
                return RedirectToAction("EndGame", new { reason = "win" });
            }

            ViewBag.Score = HttpContext.Session.GetInt32("Score") ?? 0;
            ViewBag.Money = HttpContext.Session.GetInt32("Money") ?? 0;

            return View(question);
        }

        [HttpPost]
        public IActionResult CheckAnswer(int questionId, string answer, int timeUsed)
        {
            // Chặn ngay lập tức nếu mất Session
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var question = _context.Questions.FirstOrDefault(q => q.Id == questionId);
            if (question == null) return RedirectToAction("Index", "Home");

            if (string.IsNullOrEmpty(answer)) return RedirectToAction("EndGame", new { reason = "lose" });

            int totalTime = (HttpContext.Session.GetInt32("TotalTime") ?? 0) + timeUsed;
            HttpContext.Session.SetInt32("TotalTime", totalTime);

            int currentLevel = HttpContext.Session.GetInt32("CurrentLevel") ?? 1;

            string dbAnswer = question.CorrectAnswer?.Trim().ToUpper() ?? "";
            string userAnswer = answer.Split(',')[0].Trim().ToUpper();

            if (dbAnswer == userAnswer)
            {
                int score = (HttpContext.Session.GetInt32("Score") ?? 0) + 200;
                int money = currentLevel < moneyMilestones.Length ? moneyMilestones[currentLevel] : moneyMilestones.Last();

                currentLevel++;

                if (currentLevel > 15)
                {
                    HttpContext.Session.SetInt32("Score", score);
                    HttpContext.Session.SetInt32("Money", moneyMilestones[15]);
                    return RedirectToAction("EndGame", new { reason = "win" });
                }

                HttpContext.Session.SetInt32("CurrentLevel", currentLevel);
                HttpContext.Session.SetInt32("Score", score);
                HttpContext.Session.SetInt32("Money", money);

                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("EndGame", new { reason = "lose" });
            }
        }

        public IActionResult EndGame(string reason = "quit")
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            // FIX LỖI MẤT SESSION: Tránh việc không lưu mà bị đá văng ra Login
            if (userId == 0) return RedirectToAction("Login", "Account");

            int score = HttpContext.Session.GetInt32("Score") ?? 0;
            int level = (HttpContext.Session.GetInt32("CurrentLevel") ?? 1) - 1;
            int totalTime = HttpContext.Session.GetInt32("TotalTime") ?? 0;
            long money = HttpContext.Session.GetInt32("Money") ?? 0;

            if (reason == "lose" || reason == "timeout")
            {
                money = 0;
            }

            try
            {
                // Bọc Try-Catch để chắc chắn hệ thống lưu thành công
                var game = new Game
                {
                    UserId = userId,
                    Score = score,
                    LevelReached = level,
                    TotalTime = totalTime,
                    Money = money,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Games.Add(game);

                var ranking = new Ranking
                {
                    UserId = userId,
                    Score = score,
                    TimePlayed = totalTime,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Rankings.Add(ranking);

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine("LỖI LƯU DATABASE: " + ex.Message);
            }

            HttpContext.Session.Remove("CurrentLevel");
            HttpContext.Session.Remove("Score");
            HttpContext.Session.Remove("TotalTime");
            HttpContext.Session.Remove("Money");

            return RedirectToAction("Index", "History", new { userId = userId });
        }
    }
}