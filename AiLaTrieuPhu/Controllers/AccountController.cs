using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;
using Microsoft.AspNetCore.Http;
using BCrypt.Net;
using System;
using System.Linq;

namespace AiLaTrieuPhu.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= ĐĂNG NHẬP =================
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                if (user.IsLocked && user.Role != "Admin")
                {
                    ViewBag.Error = "Tài khoản của bạn đã bị khóa bởi Quản trị viên!";
                    return View();
                }

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("Username", user.Username);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
            return View();
        }

        // ================= ĐĂNG KÝ =================
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string username, string password, string email)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Tên tài khoản đã tồn tại!";
                return View();
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "Email này đã được sử dụng cho tài khoản khác!";
                return View();
            }

            var user = new User
            {
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Email = email,
                Role = "User",
                IsLocked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            ViewBag.Success = "Tài khoản của bạn đã được khởi tạo thành công!";
            return View();
        }

        // ================= CHỨC NĂNG MỚI: ĐỔI MẬT KHẨU (USER + PASS CŨ) =================

        public IActionResult ChangePassword() => View();

        [HttpPost]
        public IActionResult ChangePassword(string username, string oldPassword, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            // Bước 1: Kiểm tra tài khoản và xác thực mật khẩu cũ
            if (user == null || !BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            {
                ViewBag.Error = "Tên tài khoản hoặc mật khẩu cũ không chính xác!";
                return View();
            }

            // Bước 2: Kiểm tra độ dài mật khẩu mới
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return View();
            }

            // Bước 3: Cập nhật mật khẩu mới (Hash lại)
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.SaveChanges();

            ViewBag.Success = "Đổi mật khẩu thành công! Hãy dùng mật khẩu mới để đăng nhập.";
            return View();
        }

        // ================= QUÊN MẬT KHẨU (YÊU CẦU USER + EMAIL) =================
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public IActionResult ResetPasswordRequest(string username, string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Thông tin tài khoản hoặc Email xác thực không chính xác!";
                return View("ForgotPassword");
            }

            if (user.Role == "Admin")
            {
                ViewBag.Error = "Không thể tự động reset mật khẩu cho quản trị viên!";
                return View("ForgotPassword");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword("123456");
            _context.SaveChanges();

            ViewBag.Success = "Xác thực thành công! Mật khẩu đã được reset về: 123456";
            return View("ForgotPassword");
        }

        // ================= CẬP NHẬT EMAIL (CHO USER ĐÃ LOGIN) =================
        [HttpPost]
        public IActionResult UpdateEmail(string email)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            if (_context.Users.Any(u => u.Email == email && u.Id != userId))
            {
                return BadRequest(new { message = "Email này đã được sử dụng!" });
            }

            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.Email = email;
                _context.SaveChanges();
                return Ok(new { message = "Cập nhật Email thành công" });
            }
            return BadRequest();
        }

        // ================= ĐĂNG XUẤT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}