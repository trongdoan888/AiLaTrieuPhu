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

            // Kiểm tra user tồn tại và khớp mật khẩu (dùng BCrypt)
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                // Kiểm tra trạng thái khóa tài khoản (trừ Admin)
                if (user.IsLocked && user.Role != "Admin")
                {
                    ViewBag.Error = "Tài khoản của bạn đã bị khóa bởi Quản trị viên!";
                    return View();
                }

                // Lưu thông tin vào Session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("Username", user.Username); // Lưu thêm username để hiển thị UI

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
            // Kiểm tra trùng lặp Username hoặc Email
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
                Password = BCrypt.Net.BCrypt.HashPassword(password), // Hash mật khẩu
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

        // ================= QUÊN MẬT KHẨU (YÊU CẦU USER + EMAIL) =================
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public IActionResult ResetPasswordRequest(string username, string email)
        {
            // Kiểm tra khớp cả Tên tài khoản VÀ Email
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Thông tin tài khoản hoặc Email xác thực không chính xác!";
                return View("ForgotPassword");
            }

            // Bảo mật cho Admin
            if (user.Role == "Admin")
            {
                ViewBag.Error = "Không thể tự động reset mật khẩu cho quản trị viên!";
                return View("ForgotPassword");
            }

            // Reset về mật khẩu mặc định và Hash lại
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

            // Kiểm tra email mới có bị trùng với ai khác không
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