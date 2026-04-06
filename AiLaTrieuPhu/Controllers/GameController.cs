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

        // Mảng tiền thưởng 2 Tỷ đã được cập nhật
        private readonly long[] moneyLadder = {
            1000000, 2000000, 3000000, 5000000, 10000000,
            20000000, 35000000, 60000000, 100000000, 250000000,
            500000000, 800000000, 1200000000, 1600000000, 2000000000
        };

        public GameController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= CHẾ ĐỘ 1VS1 (PVP) =================
        // ĐÂY LÀ HÀM BỊ THIẾU GÂY RA LỖI 404 - Mình đã thêm lại
        public IActionResult PvPLobby()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        // ================= CHỌN LĨNH VỰC =================
        public IActionResult ChooseCategory()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            int currentLevel = HttpContext.Session.GetInt32("CurrentLevel") ?? 1;

            // HỒI TRỢ GIÚP TẠI MỐC 5 VÀ 10
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

            if (string.IsNullOrEmpty(category))
                return RedirectToAction("ChooseCategory");

            int currentLevel = HttpContext.Session.GetInt32("CurrentLevel") ?? 1;

            if (currentLevel == 1)
            {
                HttpContext.Session.SetInt32("Score", 0);
                HttpContext.Session.SetInt32("TotalTime", 0);

                // Lưu tiền dạng String để tránh lỗi tràn số (Overflow)
                HttpContext.Session.SetString("Money", "0");

                HttpContext.Session.Remove("Used_5050");
                HttpContext.Session.Remove("Used_Call");
                HttpContext.Session.Remove("Used_Audience");
            }

            var question = _context.Questions
                .Where(q => q.Level == currentLevel && q.Category == category)
                .OrderBy(q => Guid.NewGuid())
                .FirstOrDefault();

            if (question == null)
            {
                return RedirectToAction("EndGame", new { reason = "win" });
            }

            // Đọc tiền từ Session
            long currentMoney = 0;
            long.TryParse(HttpContext.Session.GetString("Money"), out currentMoney);

            ViewBag.Score = HttpContext.Session.GetInt32("Score") ?? 0;
            ViewBag.Money = currentMoney;
            ViewBag.CurrentLevel = currentLevel;
            ViewBag.Category = category;

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

                // ĐÃ SỬA: Lấy đúng số tiền từ mảng moneyLadder
                long currentReward = moneyLadder[currentLevel - 1];

                currentLevel++;

                if (currentLevel > 15)
                {
                    HttpContext.Session.SetInt32("Score", score);
                    HttpContext.Session.SetString("Money", currentReward.ToString());
                    return RedirectToAction("EndGame", new { reason = "win" });
                }

                HttpContext.Session.SetInt32("CurrentLevel", currentLevel);
                HttpContext.Session.SetInt32("Score", score);
                HttpContext.Session.SetString("Money", currentReward.ToString()); // Lưu tiền mới

                return RedirectToAction("ChooseCategory");
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
            int levelReached = (HttpContext.Session.GetInt32("CurrentLevel") ?? 1);
            int totalTime = HttpContext.Session.GetInt32("TotalTime") ?? 0;

            // Đọc tiền từ Session
            long money = 0;
            long.TryParse(HttpContext.Session.GetString("Money"), out money);

            if (reason == "timeout") totalTime += 60;

            // ĐÃ SỬA: Tính toán rớt mốc dựa trên mảng moneyLadder (Mốc 5 là index 4, Mốc 10 là index 9)
            if (reason == "lose" || reason == "timeout")
            {
                if (levelReached > 10) money = moneyLadder[9]; // Rớt về mốc 10 (250 triệu)
                else if (levelReached > 5) money = moneyLadder[4]; // Rớt về mốc 5 (10 triệu)
                else money = 0; // Trắng tay
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

            // Xóa Session
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

        public IActionResult PvPPlaying(string roomId)
        {
            // Kiểm tra đăng nhập
            if (HttpContext.Session.GetInt32("UserId") == null) return RedirectToAction("Login", "Account");
            return View();
        }
    }
}