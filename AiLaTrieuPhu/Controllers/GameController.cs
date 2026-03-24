using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AiLaTrieuPhu.Controllers
{
    public class GameController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Giữ nguyên các mốc tiền thưởng cũ của bạn
        private readonly int[] moneyMilestones = { 0, 200, 400, 600, 1000, 2000, 3000, 6000, 10000, 14000, 22000, 30000, 40000, 60000, 85000, 150000 };

        public GameController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= CHỨC NĂNG MỚI: CHỌN LĨNH VỰC =================
        public IActionResult ChooseCategory()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            int currentLevel = HttpContext.Session.GetInt32("CurrentLevel") ?? 1;

            // --- TÍNH NĂNG MỚI: HỒI TRỢ GIÚP TẠI MỐC 5 VÀ 10 ---
            // Khi bắt đầu câu 6 (vừa qua mốc 5) hoặc câu 11 (vừa qua mốc 10)
            if (currentLevel == 6 || currentLevel == 11)
            {
                HttpContext.Session.Remove("Used_5050");
                HttpContext.Session.Remove("Used_Call");
                HttpContext.Session.Remove("Used_Audience");
            }

            var categories = new List<string> { "Toán học", "Văn học", "Khoa học tự nhiên", "Khoa học xã hội" };
            ViewBag.CurrentLevel = currentLevel;
            return View(categories);
        }

        public IActionResult Index(string category)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            // Nếu chưa chọn lĩnh vực, bắt quay lại trang chọn
            if (string.IsNullOrEmpty(category))
                return RedirectToAction("ChooseCategory");

            int currentLevel = HttpContext.Session.GetInt32("CurrentLevel") ?? 1;

            if (currentLevel == 1)
            {
                HttpContext.Session.SetInt32("Score", 0);
                HttpContext.Session.SetInt32("TotalTime", 0);
                HttpContext.Session.SetInt32("Money", 0);
                // Level 1 thì reset trợ giúp (đề phòng ván trước còn sót)
                HttpContext.Session.Remove("Used_5050");
                HttpContext.Session.Remove("Used_Call");
                HttpContext.Session.Remove("Used_Audience");
            }

            // --- TÍNH NĂNG MỚI: LẤY CÂU HỎI THEO LEVEL + CATEGORY ---
            var question = _context.Questions
                .Where(q => q.Level == currentLevel && q.Category == category)
                .OrderBy(q => Guid.NewGuid())
                .FirstOrDefault();

            if (question == null)
            {
                return RedirectToAction("EndGame", new { reason = "win" });
            }

            // Giữ nguyên các ViewBag cũ để hiển thị giao diện
            ViewBag.Score = HttpContext.Session.GetInt32("Score") ?? 0;
            ViewBag.Money = HttpContext.Session.GetInt32("Money") ?? 0;
            ViewBag.CurrentLevel = currentLevel;
            ViewBag.Category = category; // Gửi category qua để hiển thị trên UI

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

            int totalTime = (HttpContext.Session.GetInt32("TotalTime") ?? 0) + timeUsed;
            HttpContext.Session.SetInt32("TotalTime", totalTime);

            int currentLevel = HttpContext.Session.GetInt32("CurrentLevel") ?? 1;
            string dbAnswer = question.CorrectAnswer?.Trim().ToUpper() ?? "";
            string userAnswer = answer?.Split(',')[0].Trim().ToUpper() ?? "";

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

                // TRẢ LỜI ĐÚNG -> QUAY VỀ CHỌN LĨNH VỰC CHO CÂU TIẾP THEO
                return RedirectToAction("ChooseCategory");
            }
            else
            {
                return RedirectToAction("EndGame", new { reason = "lose" });
            }
        }

        // Hàm EndGame và UseLifeline giữ nguyên 100% code cũ của bạn
        public IActionResult EndGame(string reason = "quit")
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return RedirectToAction("Login", "Account");

            int score = HttpContext.Session.GetInt32("Score") ?? 0;
            int levelReached = (HttpContext.Session.GetInt32("CurrentLevel") ?? 1);
            int totalTime = HttpContext.Session.GetInt32("TotalTime") ?? 0;
            long money = HttpContext.Session.GetInt32("Money") ?? 0;

            if (reason == "timeout") totalTime += 60;

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
                    _context.Rankings.Add(new Ranking { UserId = userId, Score = score, TimePlayed = totalTime, CreatedAt = DateTime.UtcNow });
                }
                _context.SaveChanges();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            // Xóa Session sau khi kết thúc
            HttpContext.Session.Remove("CurrentLevel");
            HttpContext.Session.Remove("Score");
            HttpContext.Session.Remove("TotalTime");
            HttpContext.Session.Remove("Money");
            HttpContext.Session.Remove("Used_5050");
            HttpContext.Session.Remove("Used_Call");
            HttpContext.Session.Remove("Used_Audience");

            ViewBag.Reason = reason;
            ViewBag.Money = money;
            ViewBag.TotalTime = totalTime;
            ViewBag.LevelReached = levelReached;

            return View("EndGame");
        }

        [HttpPost]
        public IActionResult UseLifeline(string type)
        {
            HttpContext.Session.SetString("Used_" + type, "true");
            return Ok();
        }
    }
}