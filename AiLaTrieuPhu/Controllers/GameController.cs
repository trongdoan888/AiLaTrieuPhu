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

        // Các mốc tiền thưởng cho 15 Level
        private readonly int[] moneyMilestones = { 0, 200, 400, 600, 1000, 2000, 3000, 6000, 10000, 14000, 22000, 30000, 40000, 60000, 85000, 150000 };

        public GameController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            // Lấy Level hiện tại (từ 1 đến 15)
            int currentLevel = HttpContext.Session.GetInt32("CurrentLevel") ?? 1;

            if (currentLevel == 1)
            {
                HttpContext.Session.SetInt32("Score", 0);
                HttpContext.Session.SetInt32("TotalTime", 0);
                HttpContext.Session.SetInt32("Money", 0);
                HttpContext.Session.Remove("Used_5050");
                HttpContext.Session.Remove("Used_Call");
                HttpContext.Session.Remove("Used_Audience");
            }

            // LOGIC CHUẨN: Lấy ngẫu nhiên 1 câu hỏi tương ứng đúng với Level hiện tại
            var question = _context.Questions
                .Where(q => q.Level == currentLevel)
                .OrderBy(q => Guid.NewGuid())
                .FirstOrDefault();

            if (question == null)
            {
                return RedirectToAction("EndGame", new { reason = "win" });
            }

            ViewBag.Score = HttpContext.Session.GetInt32("Score") ?? 0;
            ViewBag.Money = HttpContext.Session.GetInt32("Money") ?? 0;
            ViewBag.CurrentLevel = currentLevel; // Truyền Level sang giao diện

            ViewBag.Used5050 = HttpContext.Session.GetString("Used_5050") == "true";
            ViewBag.UsedCall = HttpContext.Session.GetString("Used_Call") == "true";
            ViewBag.UsedAudience = HttpContext.Session.GetString("Used_Audience") == "true";

            return View(question);
        }

        [HttpPost]
        public IActionResult CheckAnswer(int questionId, string answer, int timeUsed)
        {
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

                currentLevel++; // TRẢ LỜI ĐÚNG -> TĂNG LÊN LEVEL TIẾP THEO LUÔN

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
            if (userId == 0) return RedirectToAction("Login", "Account");

            int score = HttpContext.Session.GetInt32("Score") ?? 0;
            // LƯU TRỰC TIẾP SỐ LEVEL ĐÃ VƯỢT QUA (Từ 0 đến 15)
            int levelReached = (HttpContext.Session.GetInt32("CurrentLevel") ?? 1) - 1;

            int totalTime = HttpContext.Session.GetInt32("TotalTime") ?? 0;
            long money = HttpContext.Session.GetInt32("Money") ?? 0;

            if (reason == "timeout")
            {
                totalTime += 60; // Hết giờ thì cộng thêm 60s vào thời gian
            }

            // Mốc tiền thưởng an toàn khi thua
            if (reason == "lose" || reason == "timeout")
            {
                if (levelReached >= 10) money = moneyMilestones[10];
                else if (levelReached >= 5) money = moneyMilestones[5];
                else money = 0;
            }

            try
            {
                var game = new Game
                {
                    UserId = userId,
                    Score = score,
                    LevelReached = levelReached,
                    TotalTime = totalTime,
                    Money = money,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Games.Add(game);

                var existingRanking = _context.Rankings.FirstOrDefault(r => r.UserId == userId);
                if (existingRanking != null)
                {
                    if (score > existingRanking.Score || (score == existingRanking.Score && totalTime < existingRanking.TimePlayed))
                    {
                        existingRanking.Score = score;
                        existingRanking.TimePlayed = totalTime;
                        existingRanking.CreatedAt = DateTime.UtcNow;
                        _context.Rankings.Update(existingRanking);
                    }
                }
                else
                {
                    var ranking = new Ranking
                    {
                        UserId = userId,
                        Score = score,
                        TimePlayed = totalTime,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Rankings.Add(ranking);
                }

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
            HttpContext.Session.Remove("Used_5050");
            HttpContext.Session.Remove("Used_Call");
            HttpContext.Session.Remove("Used_Audience");

            return RedirectToAction("Index", "History", new { userId = userId });
        }

        [HttpPost]
        public IActionResult UseLifeline(string type)
        {
            HttpContext.Session.SetString("Used_" + type, "true");
            return Ok();
        }
    }
}