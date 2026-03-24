using System;
using System.Collections.Generic;

namespace AiLaTrieuPhu.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "User";

        // Thêm trường mới để quản lý trạng thái khóa tài khoản
        // false: Bình thường | true: Bị khóa
        public bool IsLocked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Game>? Games { get; set; }

        public string? Email { get; set; } // Cho phép null để xử lý các tài khoản cũ chưa có email
                                           // ...
    }
}