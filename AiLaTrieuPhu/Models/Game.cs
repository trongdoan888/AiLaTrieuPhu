namespace AiLaTrieuPhu.Models
{
    public class Game
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Score { get; set; }
        public int LevelReached { get; set; }
        public int TotalTime { get; set; }
        public long Money { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User? User { get; set; }
        public ICollection<GameDetail>? Details { get; set; }
    }
}