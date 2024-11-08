using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace backend_api.SignalR
{
    public class NotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            UserConnections[userId] = Context.ConnectionId;

            Console.WriteLine($"Connected: {userId}, ConnectionId: {Context.ConnectionId}"); // Log the connection
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            if (UserConnections.TryRemove(userId, out var connectionId))
            {
                Console.WriteLine($"Disconnected: {userId}, ConnectionId: {connectionId}"); // Log disconnection
            }

            return base.OnDisconnectedAsync(exception);
        }

        public static string? GetConnectionIdByUserId(string userId)
        {
            UserConnections.TryGetValue(userId, out var connectionId);
            return connectionId;
        }
    }
}
