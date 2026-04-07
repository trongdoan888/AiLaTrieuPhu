namespace AiLaTrieuPhu.Models
{
    public class GameRoom
    {
        // ... (Giữ nguyên các biến cũ)
        public string RoomId { get; set; }
        public string Player1ConnectionId { get; set; }
        public string Player2ConnectionId { get; set; }
        public string Player1Name { get; set; }
        public string Player2Name { get; set; }

        public bool IsP1Ready { get; set; }
        public bool IsP2Ready { get; set; }
        public bool IsStarted { get; set; }
        public bool IsPrivate { get; set; }

        public int CurrentLevel { get; set; } = 1;
        public int Player1Score { get; set; } = 0;
        public int Player2Score { get; set; } = 0;
        public string CurrentCorrectAnswer { get; set; }
        public bool IsRoundFinished { get; set; }
        public int CurrentQuestionId { get; set; } = 0;
        public int WrongAnswersCount { get; set; } = 0;

        // === THÊM BIẾN NÀY ĐỂ LƯU TIỀN CƯỢC ===
        public long BetAmount { get; set; } = 0;
    }
}