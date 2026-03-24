namespace AiLaTrieuPhu.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public string A { get; set; } = "";
        public string B { get; set; } = "";
        public string C { get; set; } = "";
        public string D { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
        public int Level { get; set; }

        // --- THÊM 2 CỘT MỚI ---
        public string Hint { get; set; } = ""; // Gợi ý
        public string Category { get; set; } = ""; // Dạng câu hỏi (Lịch sử, Toán học, Giải trí...)
    }
}