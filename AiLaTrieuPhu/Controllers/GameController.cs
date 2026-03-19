using Microsoft.AspNetCore.Mvc;
using AiLaTrieuPhu.Data;
using AiLaTrieuPhu.Models;

public class GameController : Controller
{
    private readonly ApplicationDbContext _context;

    public GameController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var question = _context.Questions.FirstOrDefault(q => q.Level == 1);
        return View(question);
    }

    [HttpPost]
    public IActionResult EndGame(int userId, int score, int level, int totalTime, long money)
    {
        var game = new Game
        {
            UserId = userId,
            Score = score,
            LevelReached = level,
            TotalTime = totalTime,
            Money = money
        };

        _context.Games.Add(game);
        _context.SaveChanges();

        var ranking = new Ranking
        {
            UserId = userId,
            Score = score,
            TimePlayed = totalTime
        };

        _context.Rankings.Add(ranking);
        _context.SaveChanges();

        return RedirectToAction("Index", "Home");
    }
}