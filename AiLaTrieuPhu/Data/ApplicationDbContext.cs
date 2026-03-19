using Microsoft.EntityFrameworkCore;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<GameDetail> GameDetails { get; set; }
        public DbSet<Ranking> Rankings { get; set; }
    }
}