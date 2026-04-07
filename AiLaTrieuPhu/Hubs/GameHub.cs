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

        // --- HỆ THỐNG GHÉP PHÒNG ---
        public async Task JoinRandom(string playerName)
        {
            var room = Rooms.Values.FirstOrDefault(r => !r.IsPrivate && !r.IsStarted && r.Player2ConnectionId == null);
            if (room == null)
            {
                string newId = "R" + new Random().Next(1000, 9999);
                room = new GameRoom { RoomId = newId, Player1ConnectionId = Context.ConnectionId, Player1Name = playerName, IsPrivate = false };
                Rooms[newId] = room;
                await Groups.AddToGroupAsync(Context.ConnectionId, newId);
                await Clients.Caller.SendAsync("RoomCreated", newId);
            }
            else
            {
                room.Player2ConnectionId = Context.ConnectionId;
                room.Player2Name = playerName;
                await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);
                await Clients.Group(room.RoomId).SendAsync("UpdateRoomStatus", room.Player1Name, room.Player2Name, room.RoomId);
            }
        }

        public async Task CreatePrivateRoom(string playerName)
        {
            string roomId = new Random().Next(100000, 999999).ToString();
            var room = new GameRoom { RoomId = roomId, Player1ConnectionId = Context.ConnectionId, Player1Name = playerName, IsPrivate = true };
            Rooms[roomId] = room;
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("RoomCreated", roomId);
        }

        public async Task JoinPrivateRoom(string roomId, string playerName)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                if (room.Player2ConnectionId == null)
                {
                    room.Player2ConnectionId = Context.ConnectionId;
                    room.Player2Name = playerName;
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                    await Clients.Group(roomId).SendAsync("UpdateRoomStatus", room.Player1Name, room.Player2Name, roomId);
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
                    room.WrongAnswersCount = 0; // Khởi tạo biến đếm
                    await Clients.Group(roomId).SendAsync("StartGameCountdown");
                }
            }
        }

        // --- TRẬN ĐẤU: VÀO LÀ CHIẾN LUÔN ---
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
                            room.WrongAnswersCount = 0;
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

        // --- ĐÃ SỬA: ĐẾM SỐ NGƯỜI SAI & TRUYỀN ĐÚNG ĐIỂM ---
        public async Task SubmitPvPAnswer(string roomId, string playerName, string answer)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                if (room.IsRoundFinished) return;

                bool isCorrect = (answer.Trim().ToUpper() == room.CurrentCorrectAnswer);

                if (isCorrect)
                {
                    room.IsRoundFinished = true; // Có người đúng -> Khóa vòng

                    string winnerConnectionId = Context.ConnectionId;
                    string loserConnectionId = (winnerConnectionId == room.Player1ConnectionId) ? room.Player2ConnectionId : room.Player1ConnectionId;

                    if (winnerConnectionId == room.Player1ConnectionId) room.Player1Score += 1000;
                    else room.Player2Score += 1000;

                    // Gửi toàn bộ tên và điểm của 2 người để UI tự phân loại
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
                    room.WrongAnswersCount++; // Tăng số người trả lời sai

                    if (Context.ConnectionId == room.Player1ConnectionId) room.Player1Score -= 1000;
                    else room.Player2Score -= 1000;

                    if (room.WrongAnswersCount >= 2)
                    {
                        // Nếu CẢ 2 NGƯỜI đều sai -> Khóa vòng, báo lỗi, chuyển câu
                        room.IsRoundFinished = true;
                        await Clients.Group(roomId).SendAsync("BothWrong", room.CurrentCorrectAnswer, room.Player1Name, room.Player1Score, room.Player2Name, room.Player2Score);

                        await Task.Delay(3000);
                        await NextQuestion(room);
                    }
                    else
                    {
                        // 1 người sai -> Cập nhật điểm cho cả phòng, báo hiệu ứng đỏ cho người sai
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
                string winner = (room.Player1Score > room.Player2Score) ? room.Player1Name :
                                (room.Player1Score < room.Player2Score) ? room.Player2Name : "HÒA NHAU!";
                await Clients.Group(room.RoomId).SendAsync("GameOver", winner);
                return;
            }

            var question = _context.Questions.Where(q => q.Level == room.CurrentLevel).OrderBy(q => Guid.NewGuid()).FirstOrDefault();
            if (question != null)
            {
                room.CurrentQuestionId = question.Id;
                room.CurrentCorrectAnswer = question.CorrectAnswer.Trim().ToUpper();
                room.IsRoundFinished = false;
                room.WrongAnswersCount = 0; // Reset số người sai cho câu mới

                await Clients.Group(room.RoomId).SendAsync("ReceiveQuestion", room.CurrentLevel, question.Content, question.A, question.B, question.C, question.D);
            }
        }

        // --- ĐÃ SỬA: CHỈ ÁP DỤNG TRỢ GIÚP CHO NGƯỜI BẤM ---
        public async Task UseLifelinePvP(string roomId, string type)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                // Dùng Clients.Caller thay vì Clients.Group để chỉ người bấm mới thấy tác dụng
                await Clients.Caller.SendAsync("ApplyLifeline", type, room.CurrentCorrectAnswer);
            }
        }
    }
}