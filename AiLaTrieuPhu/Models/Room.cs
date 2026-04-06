namespace AiLaTrieuPhu.Models
{
    public class GameRoom
    {
        public string RoomId { get; set; }
        public string Player1ConnectionId { get; set; }
        public string Player2ConnectionId { get; set; }
        public string Player1Name { get; set; }
        public string Player2Name { get; set; }
        public bool IsP1Ready { get; set; } // Người 1 sẵn sàng chưa
        public bool IsP2Ready { get; set; } // Người 2 sẵn sàng chưa
        public bool IsStarted { get; set; }
        public bool IsPrivate { get; set; }
    }
}