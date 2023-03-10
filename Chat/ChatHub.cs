using Microsoft.AspNetCore.SignalR;

namespace Chat;

public class ChatRoom
{
    public string RoomName { get; set; }
    public List<string> ConnectedClients { get; set; } = new();
    public List<string> MessageHistory { get; set; } = new();
}

public class ChatHub : Hub
{
    private readonly List<ChatRoom> _chatRooms;

    public ChatHub(List<ChatRoom> chatRooms)
    {
        _chatRooms = chatRooms;
    }

    public override async Task OnConnectedAsync()
    {
        await JoinRoom("Default");
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        //remove from rooms
        await base.OnDisconnectedAsync(exception);
    }


    public async Task JoinRoom(string roomName)
    {
        var connectionId = Context.ConnectionId;

        var chatRoom = _chatRooms.FirstOrDefault(x => x.RoomName == roomName);
        if (chatRoom == null)
        {
            chatRoom = new ChatRoom
            {
                RoomName = roomName
            };
            _chatRooms.Add(chatRoom);
        }

        chatRoom.ConnectedClients.Add(connectionId);

        await Groups.AddToGroupAsync(connectionId, roomName);

        if (chatRoom.MessageHistory.Count > 0)
        {
            await Clients.Caller.SendAsync("ReceiveMessageHistory", chatRoom.MessageHistory);
        }

        await Clients.Group(roomName).SendAsync("ReceiveMessage", $"{connectionId} has joined the chat.");
    }

    public async Task LeaveRoom(string roomName)
    {
        var connectionId = Context.ConnectionId;

        var chatRoom = _chatRooms.FirstOrDefault(x => x.RoomName == roomName);
        if (chatRoom != null)
        {
            chatRoom.ConnectedClients.Remove(connectionId);

            if (chatRoom.ConnectedClients.Count == 0)
            {
                _chatRooms.Remove(chatRoom);
            }
        }

        await Groups.RemoveFromGroupAsync(connectionId, roomName);

        await Clients.Group(roomName).SendAsync("ReceiveMessage", $"{connectionId} has left the chat.");
    }

    public async Task SendMessage(string roomName, string message)
    {
        var connectionId = Context.ConnectionId;

        var chatRoom = _chatRooms.FirstOrDefault(x => x.RoomName == roomName);
        if (chatRoom != null)
        {
            chatRoom.MessageHistory.Add($"{connectionId}: {message}");
        }

        await Clients.Group(roomName).SendAsync("ReceiveMessage", $"{connectionId}: {message}");
    }
}