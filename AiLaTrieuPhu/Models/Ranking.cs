namespace AiLaTrieuPhu.Models
{
    public class Ranking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Score { get; set; }
        public int TimePlayed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User? User { get; set; }
    }
}