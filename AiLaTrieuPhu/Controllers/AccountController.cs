using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;
using BCrypt.Net;
using System.Linq;

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
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Role", user.Role);

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
        return View();
    }

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
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return RedirectToAction("Login");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}