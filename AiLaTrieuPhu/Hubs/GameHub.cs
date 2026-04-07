using Microsoft.AspNetCore.SignalR;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Data;
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AiLaTrieuPhu.Hubs
{
    public class GameHub : Hub
    {
        private static ConcurrentDictionary<string, GameRoom> Rooms = new ConcurrentDictionary<string, GameRoom>();
        private readonly ApplicationDbContext _context;

        public GameHub(ApplicationDbContext context) { _context = context; }

        // --- HỆ THỐNG GHÉP PHÒNG CÓ TIỀN CƯỢC ---
        public async Task JoinRandom(string playerName)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == playerName);
            if (user == null) return;

            // ĐÃ SỬA: Tính tổng tiền thực tế của user từ lịch sử chơi
            long totalMoney = _context.Games.Where(g => g.UserId == user.Id).Sum(g => g.Money);

            if (totalMoney < 100000)
            {
                await Clients.Caller.SendAsync("Error", "Tài khoản của bạn không đủ 100.000 VNĐ để chơi ngẫu nhiên!");
                return;
            }

            var room = Rooms.Values.FirstOrDefault(r => !r.IsPrivate && !r.IsStarted && r.Player2ConnectionId == null);
            if (room == null)
            {
                string newId = "R" + new Random().Next(1000, 9999);
                room = new GameRoom { RoomId = newId, Player1ConnectionId = Context.ConnectionId, Player1Name = playerName, IsPrivate = false, BetAmount = 100000 };
                Rooms[newId] = room;
                await Groups.AddToGroupAsync(Context.ConnectionId, newId);
                await Clients.Caller.SendAsync("RoomCreated", newId, room.BetAmount);
            }
            else
            {
                room.Player2ConnectionId = Context.ConnectionId;
                room.Player2Name = playerName;
                await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);
                await Clients.Group(room.RoomId).SendAsync("UpdateRoomStatus", room.Player1Name, room.Player2Name, room.RoomId, room.BetAmount);
            }
        }

        public async Task CreatePrivateRoom(string playerName, long betAmount)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == playerName);
            if (user == null) return;

            // ĐÃ SỬA: Tính tổng tiền
            long totalMoney = _context.Games.Where(g => g.UserId == user.Id).Sum(g => g.Money);

            if (totalMoney < betAmount)
            {
                await Clients.Caller.SendAsync("Error", "Số dư của bạn không đủ để thiết lập mức cược này!");
                return;
            }

            string roomId = new Random().Next(100000, 999999).ToString();
            var room = new GameRoom { RoomId = roomId, Player1ConnectionId = Context.ConnectionId, Player1Name = playerName, IsPrivate = true, BetAmount = betAmount };
            Rooms[roomId] = room;
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("RoomCreated", roomId, betAmount);
        }

        public async Task JoinPrivateRoom(string roomId, string playerName)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                if (room.Player2ConnectionId == null)
                {
                    var user = _context.Users.FirstOrDefault(u => u.Username == playerName);
                    if (user == null) return;

                    // ĐÃ SỬA: Tính tổng tiền
                    long totalMoney = _context.Games.Where(g => g.UserId == user.Id).Sum(g => g.Money);

                    if (totalMoney < room.BetAmount)
                    {
                        await Clients.Caller.SendAsync("Error", $"Bạn cần ít nhất {room.BetAmount:N0} VNĐ để vào phòng này!");
                        return;
                    }

                    room.Player2ConnectionId = Context.ConnectionId;
                    room.Player2Name = playerName;
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                    await Clients.Group(roomId).SendAsync("UpdateRoomStatus", room.Player1Name, room.Player2Name, roomId, room.BetAmount);
                }
                else { await Clients.Caller.SendAsync("Error", "Phòng đầy!"); }
            }
            else { await Clients.Caller.SendAsync("Error", "Sai mã PIN!"); }
        }

        public async Task SetReady(string roomId)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                if (Context.ConnectionId == room.Player1ConnectionId) room.IsP1Ready = true;
                else if (Context.ConnectionId == room.Player2ConnectionId) room.IsP2Ready = true;

                await Clients.Group(roomId).SendAsync("PlayerReadyUpdate", room.IsP1Ready, room.IsP2Ready);

                if (room.IsP1Ready && room.IsP2Ready)
                {
                    room.IsStarted = true;
                    room.CurrentLevel = 1;
                    room.CurrentQuestionId = 0;
                    room.WrongAnswersCount = 0;
                    await Clients.Group(roomId).SendAsync("StartGameCountdown");
                }
            }
        }

        public async Task PlayerReadyForBattle(string roomId, string playerName)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                if (room.Player1Name == playerName) room.Player1ConnectionId = Context.ConnectionId;
                else room.Player2ConnectionId = Context.ConnectionId;

                string opponent = (room.Player1Name == playerName) ? room.Player2Name : room.Player1Name;
                await Clients.Caller.SendAsync("BattleStarted", opponent);

                Question currentQ = null;
                lock (room)
                {
                    if (room.CurrentQuestionId == 0)
                    {
                        currentQ = _context.Questions.Where(q => q.Level == room.CurrentLevel).OrderBy(q => Guid.NewGuid()).FirstOrDefault();
                        if (currentQ != null)
                        {
                            room.CurrentQuestionId = currentQ.Id;
                            room.CurrentCorrectAnswer = currentQ.CorrectAnswer.Trim().ToUpper();
                            room.IsRoundFinished = false;
                        }
                    }
                    else
                    {
                        currentQ = _context.Questions.FirstOrDefault(q => q.Id == room.CurrentQuestionId);
                    }
                }

                if (currentQ != null)
                {
                    await Clients.Caller.SendAsync("ReceiveQuestion", room.CurrentLevel, currentQ.Content, currentQ.A, currentQ.B, currentQ.C, currentQ.D);
                }
            }
        }

        public async Task SubmitPvPAnswer(string roomId, string playerName, string answer)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                if (room.IsRoundFinished) return;

                bool isCorrect = (answer.Trim().ToUpper() == room.CurrentCorrectAnswer);

                if (isCorrect)
                {
                    room.IsRoundFinished = true;

                    string winnerConnectionId = Context.ConnectionId;
                    string loserConnectionId = (winnerConnectionId == room.Player1ConnectionId) ? room.Player2ConnectionId : room.Player1ConnectionId;

                    if (winnerConnectionId == room.Player1ConnectionId) room.Player1Score += 1000;
                    else room.Player2Score += 1000;

                    await Clients.Caller.SendAsync("RoundResult", playerName, true, room.CurrentCorrectAnswer, room.Player1Name, room.Player1Score, room.Player2Name, room.Player2Score);

                    if (!string.IsNullOrEmpty(loserConnectionId))
                    {
                        await Clients.Client(loserConnectionId).SendAsync("OpponentWonRound", playerName, room.CurrentCorrectAnswer, room.Player1Name, room.Player1Score, room.Player2Name, room.Player2Score);
                    }

                    await Task.Delay(3000);
                    await NextQuestion(room);
                }
                else
                {
                    room.WrongAnswersCount++;

                    if (Context.ConnectionId == room.Player1ConnectionId) room.Player1Score -= 1000;
                    else room.Player2Score -= 1000;

                    if (room.WrongAnswersCount >= 2)
                    {
                        room.IsRoundFinished = true;
                        await Clients.Group(roomId).SendAsync("BothWrong", room.CurrentCorrectAnswer, room.Player1Name, room.Player1Score, room.Player2Name, room.Player2Score);

                        await Task.Delay(3000);
                        await NextQuestion(room);
                    }
                    else
                    {
                        await Clients.Group(roomId).SendAsync("UpdateScoreOnly", room.Player1Name, room.Player1Score, room.Player2Name, room.Player2Score);
                        await Clients.Caller.SendAsync("WrongAnswerFeedback", answer);
                    }
                }
            }
        }

        private async Task NextQuestion(GameRoom room)
        {
            room.CurrentLevel++;

            if (room.CurrentLevel > 15)
            {
                string winner = "HÒA NHAU!";

                var dbUser1 = _context.Users.FirstOrDefault(u => u.Username == room.Player1Name);
                var dbUser2 = _context.Users.FirstOrDefault(u => u.Username == room.Player2Name);

                if (dbUser1 != null && dbUser2 != null)
                {
                    // ĐÃ SỬA: Tạo bản ghi Lịch sử chơi (Game) cho việc Trừ/Cộng tiền cược
                    if (room.Player1Score > room.Player2Score)
                    {
                        winner = room.Player1Name;
                        // Người 1 thắng: Tiền dương
                        _context.Games.Add(new Game { UserId = dbUser1.Id, Score = room.Player1Score, LevelReached = 15, TotalTime = 0, Money = room.BetAmount, CreatedAt = DateTime.UtcNow });
                        // Người 2 thua: Tiền âm
                        _context.Games.Add(new Game { UserId = dbUser2.Id, Score = room.Player2Score, LevelReached = 15, TotalTime = 0, Money = -room.BetAmount, CreatedAt = DateTime.UtcNow });
                    }
                    else if (room.Player2Score > room.Player1Score)
                    {
                        winner = room.Player2Name;
                        // Người 1 thua: Tiền âm
                        _context.Games.Add(new Game { UserId = dbUser1.Id, Score = room.Player1Score, LevelReached = 15, TotalTime = 0, Money = -room.BetAmount, CreatedAt = DateTime.UtcNow });
                        // Người 2 thắng: Tiền dương
                        _context.Games.Add(new Game { UserId = dbUser2.Id, Score = room.Player2Score, LevelReached = 15, TotalTime = 0, Money = room.BetAmount, CreatedAt = DateTime.UtcNow });
                    }

                    // Chỉ lưu DB nếu có sự thay đổi tiền (Tức là không phải kết quả Hòa)
                    if (room.Player1Score != room.Player2Score)
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                await Clients.Group(room.RoomId).SendAsync("GameOver", winner);
                return;
            }

            var question = _context.Questions.Where(q => q.Level == room.CurrentLevel).OrderBy(q => Guid.NewGuid()).FirstOrDefault();
            if (question != null)
            {
                room.CurrentQuestionId = question.Id;
                room.CurrentCorrectAnswer = question.CorrectAnswer.Trim().ToUpper();
                room.IsRoundFinished = false;
                room.WrongAnswersCount = 0;

                await Clients.Group(room.RoomId).SendAsync("ReceiveQuestion", room.CurrentLevel, question.Content, question.A, question.B, question.C, question.D);
            }
        }

        public async Task UseLifelinePvP(string roomId, string type)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                await Clients.Caller.SendAsync("ApplyLifeline", type, room.CurrentCorrectAnswer);
            }
        }
    }
}