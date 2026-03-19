namespace AiLaTrieuPhu.Models
{
    public class GameDetail
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int QuestionId { get; set; }
        public string SelectedAnswer { get; set; } = "";
        public bool IsCorrect { get; set; }
        public int TimeUsed { get; set; }

        public Game? Game { get; set; }
        public Question? Question { get; set; }
    }
}