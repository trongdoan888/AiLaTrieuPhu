using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Data;
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
            return RedirectToAction("Login", "Account");

        return View();
    }
}