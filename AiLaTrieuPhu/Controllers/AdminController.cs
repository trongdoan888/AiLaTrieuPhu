using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.IO;
using ClosedXML.Excel;

namespace AiLaTrieuPhu.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hàm kiểm tra quyền Admin
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        // 1. TRANG CHỦ ADMIN (DASHBOARD)
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalQuestions = _context.Questions.Count();
            ViewBag.TotalGames = _context.Games.Count();
            return View();
        }

        // ================= QUẢN LÝ CÂU HỎI =================

        public IActionResult Questions()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var questions = _context.Questions.OrderBy(q => q.Level).ThenBy(q => q.Id).ToList();
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

            if (model.Id == 0) _context.Questions.Add(model); // Thêm mới
            else _context.Questions.Update(model);            // Sửa

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
            }
            return RedirectToAction("Questions");
        }

        // --- TÍNH NĂNG MỚI: NHẬP EXCEL ---
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
                        var worksheet = workbook.Worksheet(1); // Đọc Sheet đầu tiên
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ qua dòng Tiêu đề (dòng 1)

                        foreach (var row in rows)
                        {
                            var levelStr = row.Cell(1).Value.ToString();
                            if (string.IsNullOrWhiteSpace(levelStr)) continue; // Bỏ qua nếu dòng trống

                            var q = new Question
                            {
                                Level = int.Parse(levelStr),
                                Content = row.Cell(2).Value.ToString(),
                                A = row.Cell(3).Value.ToString(),
                                B = row.Cell(4).Value.ToString(),
                                C = row.Cell(5).Value.ToString(),
                                D = row.Cell(6).Value.ToString(),
                                CorrectAnswer = row.Cell(7).Value.ToString().Trim().ToUpper()
                            };
                            _context.Questions.Add(q);
                        }
                        _context.SaveChanges();
                    }
                }
                TempData["Success"] = "Nhập câu hỏi từ Excel thành công!";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = "Lỗi đọc file Excel. Vui lòng kiểm tra lại định dạng! Chi tiết: " + ex.Message;
            }

            return RedirectToAction("Questions");
        }

        // ================= QUẢN LÝ TÀI KHOẢN =================

        public IActionResult Users()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var users = _context.Users.OrderByDescending(u => u.CreatedAt).ToList();
            return View(users);
        }

        public IActionResult DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            var user = _context.Users.Find(id);
            if (user != null && user.Id != HttpContext.Session.GetInt32("UserId"))
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
            return RedirectToAction("Users");
        }
    }
}