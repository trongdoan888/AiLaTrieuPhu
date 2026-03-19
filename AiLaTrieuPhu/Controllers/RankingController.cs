using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;
using Microsoft.EntityFrameworkCore;

public class RankingController : Controller
{
    private readonly ApplicationDbContext _context;

    public RankingController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var data = _context.Rankings
            .Include(r => r.User)
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.TimePlayed)
            .ToList();

        return View(data);
    }
}