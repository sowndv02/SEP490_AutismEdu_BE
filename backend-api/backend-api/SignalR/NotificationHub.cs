using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace backend_api.SignalR
{
    public class NotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        public override Task OnConnectedAsync()
        {
            var email = SD.ADMIN_EMAIL_DEFAULT;
            if (!string.IsNullOrEmpty(email))
            {
                UserConnections[email] = Context.ConnectionId;
                Console.WriteLine($"Connected: {email}, ConnectionId: {Context.ConnectionId}"); // Log connection
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var email = SD.ADMIN_EMAIL_DEFAULT;
            if (!string.IsNullOrEmpty(email))
            {
                UserConnections.TryRemove(email, out _);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public static string? GetConnectionIdByEmail(string email)
        {
            UserConnections.TryGetValue(email, out var connectionId);
            return connectionId;
        }
    }
}
