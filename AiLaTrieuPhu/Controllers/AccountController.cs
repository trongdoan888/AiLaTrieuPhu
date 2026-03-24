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

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
            return View();
        }

        public IActionResult Register() => View();

        // CẬP NHẬT: Đăng ký yêu cầu thêm Email
        [HttpPost]
        public IActionResult Register(string username, string password, string email)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Tài khoản đã tồn tại";
                return View();
            }

            var user = new User
            {
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Email = email, // Lưu Email
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
            // CẬP NHẬT: Kiểm tra khớp cả Tên tài khoản VÀ Email
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Thông tin tài khoản hoặc Email xác thực không chính xác!";
                return View("ForgotPassword");
            }

            if (user.Role == "Admin")
            {
                ViewBag.Error = "Không thể reset mật khẩu cho tài khoản quản trị viên!";
                return View("ForgotPassword");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword("123456");
            _context.SaveChanges();

            ViewBag.Success = "Xác thực thành công! Mật khẩu đã được reset về: 123456";
            return View("ForgotPassword");
        }

        // ================= CẬP NHẬT EMAIL CHO TÀI KHOẢN CŨ =================

        [HttpPost]
        public IActionResult UpdateEmail(string email)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.Email = email;
                _context.SaveChanges();
                return Ok(new { message = "Cập nhật Email thành công" });
            }
            return BadRequest();
        }

        // ===============================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}