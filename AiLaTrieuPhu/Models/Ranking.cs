namespace AiLaTrieuPhu.Models
{
    public class Ranking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Score { get; set; }
        public int TimePlayed { get; set; }

        // Sửa DateTime.Now thành DateTime.UtcNow
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}