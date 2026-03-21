namespace AiLaTrieuPhu.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "User";

        // Sửa DateTime.Now thành DateTime.UtcNow
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Game>? Games { get; set; }
    }
}