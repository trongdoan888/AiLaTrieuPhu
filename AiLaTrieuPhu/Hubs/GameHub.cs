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

        // --- CHẾ ĐỘ GHÉP NGẪU NHIÊN ---
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

                // ĐÃ SỬA: Gửi thêm room.RoomId để máy khách đồng bộ
                await Clients.Group(room.RoomId).SendAsync("UpdateRoomStatus", room.Player1Name, room.Player2Name, room.RoomId);
            }
        }

        // --- TẠO PHÒNG RIÊNG ---
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

                    // ĐÃ SỬA: Gửi thêm roomId để cả 2 máy cùng thấy mã phòng
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
                    await Clients.Group(roomId).SendAsync("StartGameCountdown");
                }
            }
        }

        public async Task PlayerReadyForBattle(string roomId, string playerName)
        {
            if (Rooms.TryGetValue(roomId, out var room))
            {
                var question = _context.Questions.Where(q => q.Level == 1).OrderBy(q => Guid.NewGuid()).FirstOrDefault();
                if (question != null)
                {
                    string opponent = (room.Player1Name == playerName) ? room.Player2Name : room.Player1Name;
                    await Clients.Caller.SendAsync("BattleStarted", opponent);
                    await Clients.Caller.SendAsync("ReceiveQuestion", 1, question.Content, question.A, question.B, question.C, question.D);
                }
            }
        }

        // Tương lai code tiếp logic tính điểm tại đây
        public async Task SubmitPvPAnswer(string roomId, string playerName, string answer) { }
    }
}