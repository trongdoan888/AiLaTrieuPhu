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

        // ================= CỔNG ĐĂNG NHẬP NGƯỜI CHƠI =================
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                // Chặn Admin đăng nhập ở cổng người chơi thường
                if (user.Role == "Admin")
                {
                    ViewBag.Error = "Tài khoản Quản trị vui lòng truy cập cổng Admin!";
                    return View();
                }

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Role", user.Role);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
            return View();
        }

        // ================= CỔNG ĐĂNG NHẬP QUẢN TRỊ VIÊN =================
        public IActionResult AdminLogin() => View();

        [HttpPost]
        public IActionResult AdminLogin(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                // CHỈ CHO PHÉP TÀI KHOẢN CÓ ROLE LÀ "Admin"
                if (user.Role == "Admin")
                {
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("Role", user.Role);

                    // Đăng nhập thành công -> Bay thẳng vào trang Dashboard Quản trị
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    // User thường cố tình mò vào cổng Admin
                    ViewBag.Error = "Tài khoản này không có quyền Quản trị viên!";
                    return View();
                }
            }

            ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
            return View();
        }

        // ================= ĐĂNG KÝ VÀ ĐĂNG XUẤT =================
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string username, string password)
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
                Role = "User", // Mặc định ai đăng ký cũng là User
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // --- ĐIỂM THAY ĐỔI Ở ĐÂY ---
            // Gán thông báo thành công và giữ người dùng ở lại trang để xem Popup, 
            // Popup sẽ có nút chuyển sang trang Đăng nhập
            ViewBag.Success = "Tài khoản của bạn đã được khởi tạo thành công!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}