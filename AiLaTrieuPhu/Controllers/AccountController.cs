using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;
using Microsoft.AspNetCore.Http;
using BCrypt.Net;
using System;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using MailKit.Security;

namespace AiLaTrieuPhu.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AccountController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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

        // ================= ĐỔI MẬT KHẨU (USER ĐÃ LOGIN) =================
        public IActionResult ChangePassword() => View();

        [HttpPost]
        public IActionResult ChangePassword(string username, string oldPassword, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(oldPassword, user.Password))
            {
                ViewBag.Error = "Tên tài khoản hoặc mật khẩu cũ không chính xác!";
                return View();
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                return View();
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.SaveChanges();

            ViewBag.Success = "Đổi mật khẩu thành công! Hãy dùng mật khẩu mới để đăng nhập.";
            return View();
        }

        // ================= CẬP NHẬT EMAIL =================
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

        // ================= QUÊN MẬT KHẨU (XÁC THỰC OTP QUA EMAIL) =================
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> SendOTP(string username, string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Email == email);

            if (user == null || user.Role == "Admin")
            {
                ViewBag.Error = "Thông tin tài khoản hoặc Email không chính xác!";
                return View("ForgotPassword");
            }

            // 1. Tạo mã OTP 6 số
            string otpCode = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("ResetOTP", otpCode);
            HttpContext.Session.SetString("ResetUser", username);

            // 2. Gửi Email
            try
            {
                await SendEmailAsync(email, "Mã xác nhận Reset mật khẩu",
                    $"Mã OTP của bạn là: <b style='font-size: 20px; color: #00d4ff;'>{otpCode}</b>. Vui lòng nhập mã này để tiếp tục.");

                ViewBag.Success = "Mã OTP đã được gửi đến Email của bạn!";
                return View("VerifyOTP");
            }
            catch (Exception ex)
            {
                // In lỗi chi tiết ra Terminal để kiểm tra
                Console.WriteLine("\n=== LỖI SMTP CHI TIẾT ===");
                Console.WriteLine(ex.Message);
                Console.WriteLine("==========================\n");

                ViewBag.Error = "Có lỗi xảy ra khi gửi Email. Vui lòng kiểm tra lại cấu hình SMTP!";
                return View("ForgotPassword");
            }
        }

        [HttpPost]
        public IActionResult ConfirmOTP(string otpInput)
        {
            string sessionOTP = HttpContext.Session.GetString("ResetOTP");
            string username = HttpContext.Session.GetString("ResetUser");

            if (string.IsNullOrEmpty(sessionOTP) || sessionOTP != otpInput)
            {
                ViewBag.Error = "Mã OTP không chính xác hoặc đã hết hạn!";
                return View("VerifyOTP");
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                // Cấp mật khẩu mới ngẫu nhiên
                string newPass = new Random().Next(100000, 999999).ToString();
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPass);
                _context.SaveChanges();

                HttpContext.Session.Remove("ResetOTP");
                ViewBag.NewPassword = newPass;
                return View("ResetSuccess");
            }

            return RedirectToAction("Login");
        }

        // Hàm bổ trợ gửi Email - Đã cập nhật sửa lỗi Revocation
        private async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailSettings = _config.GetSection("EmailSettings");
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
            mimeMessage.To.Add(new MailboxAddress("", email));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new TextPart("html") { Text = message };

            using var client = new SmtpClient();

            // SỬA LỖI TẠI ĐÂY: Bỏ qua kiểm tra thu hồi chứng chỉ SSL để tránh lỗi Revocation trên Windows
            client.CheckCertificateRevocation = false;

            // Sử dụng cổng 587 với StartTls
            await client.ConnectAsync(emailSettings["SmtpServer"], int.Parse(emailSettings["SmtpPort"]), SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["Password"]);
            await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);
        }

        // ================= ĐĂNG XUẤT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}