using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using ClosedXML.Excel;
using System;

namespace AiLaTrieuPhu.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        // 1. DASHBOARD
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalQuestions = _context.Questions.Count();
            ViewBag.TotalGames = _context.Games.Count();
            return View();
        }

        // ================= QUẢN LÝ CÂU HỎI (CẬP NHẬT TÌM KIẾM & LỌC) =================

        public IActionResult Questions(string searchContent, string category, int? level)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var query = _context.Questions.AsQueryable();

            // 1. Tìm kiếm theo từ khóa nội dung
            if (!string.IsNullOrEmpty(searchContent))
            {
                query = query.Where(q => q.Content.Contains(searchContent));
                ViewBag.SearchContent = searchContent;
            }

            // 2. Lọc theo lĩnh vực
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(q => q.Category == category);
                ViewBag.CurrentCategory = category;
            }

            // 3. Lọc theo cấp độ (Level)
            if (level.HasValue)
            {
                query = query.Where(q => q.Level == level.Value);
                ViewBag.CurrentLevel = level;
            }

            // Danh sách Category để hiển thị ở Dropdown ngoài View
            ViewBag.Categories = new List<string> { "Toán học", "Văn học", "Khoa học tự nhiên", "Khoa học xã hội" };

            var questions = query
                .OrderBy(q => q.Category)
                .ThenBy(q => q.Level)
                .ToList();

            return View(questions);
        }

        public IActionResult CreateQuestion()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            return View("QuestionForm", new Question());
        }

        public IActionResult EditQuestion(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var q = _context.Questions.Find(id);
            if (q == null) return NotFound();
            return View("QuestionForm", q);
        }

        [HttpPost]
        public IActionResult SaveQuestion(Question model)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            if (model.Id == 0)
            {
                _context.Questions.Add(model);
                TempData["Success"] = "Thêm câu hỏi mới thành công!";
            }
            else
            {
                _context.Questions.Update(model);
                TempData["Success"] = "Cập nhật câu hỏi thành công!";
            }

            _context.SaveChanges();
            return RedirectToAction("Questions");
        }

        public IActionResult DeleteQuestion(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var q = _context.Questions.Find(id);
            if (q != null)
            {
                _context.Questions.Remove(q);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa câu hỏi!";
            }
            return RedirectToAction("Questions");
        }

        [HttpPost]
        public IActionResult ImportExcel(IFormFile file)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file Excel!";
                return RedirectToAction("Questions");
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                        foreach (var row in rows)
                        {
                            var cat = row.Cell(1).Value.ToString();
                            if (string.IsNullOrWhiteSpace(cat)) continue;

                            var q = new Question
                            {
                                Category = cat,
                                Level = int.Parse(row.Cell(2).Value.ToString()),
                                Content = row.Cell(3).Value.ToString(),
                                A = row.Cell(4).Value.ToString(),
                                B = row.Cell(5).Value.ToString(),
                                C = row.Cell(6).Value.ToString(),
                                D = row.Cell(7).Value.ToString(),
                                CorrectAnswer = row.Cell(8).Value.ToString().Trim().ToUpper(),
                                Hint = row.Cell(9).Value.ToString()
                            };
                            _context.Questions.Add(q);
                        }
                        _context.SaveChanges();
                    }
                }
                TempData["Success"] = "Nhập bộ câu hỏi mới từ Excel thành công!";
            }
            catch (Exception)
            {
                TempData["Error"] = "Lỗi định dạng Excel! Hãy đảm bảo đủ 9 cột theo mẫu.";
            }

            return RedirectToAction("Questions");
        }

        // ================= QUẢN LÝ TÀI KHOẢN =================

        public IActionResult Users(string searchTerm)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Username.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            var users = query.OrderByDescending(u => u.CreatedAt).ToList();
            return View(users);
        }

        [HttpPost]
        public IActionResult ChangePassword(int id, string newPassword)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var user = _context.Users.Find(id);
            var currentUserId = HttpContext.Session.GetInt32("UserId");

            if (user != null && user.Id != currentUserId && user.Role != "Admin")
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                    return RedirectToAction("Users");
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _context.SaveChanges();
                TempData["Success"] = $"Đã cập nhật mật khẩu cho {user.Username}!";
            }
            else
            {
                TempData["Error"] = "Không thể đổi mật khẩu tài khoản quản trị!";
            }
            return RedirectToAction("Users");
        }

        public IActionResult ToggleLockUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var user = _context.Users.Find(id);
            var currentUserId = HttpContext.Session.GetInt32("UserId");

            if (user != null && user.Id != currentUserId && user.Role != "Admin")
            {
                user.IsLocked = !user.IsLocked;
                _context.SaveChanges();
                TempData["Success"] = user.IsLocked ? $"Đã khóa tài khoản {user.Username}" : $"Đã mở khóa tài khoản {user.Username}";
            }
            else
            {
                TempData["Error"] = "Thao tác không hợp lệ trên tài khoản quản trị!";
            }

            return RedirectToAction("Users");
        }

        public IActionResult DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var user = _context.Users.Find(id);

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (user != null && user.Id != currentUserId && user.Role != "Admin")
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa tài khoản người dùng!";
            }
            else
            {
                TempData["Error"] = "Không thể xóa tài khoản quản trị!";
            }
            return RedirectToAction("Users");
        }
    }
}