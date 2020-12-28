using System;
using System.Threading.Tasks;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly PresenceTracker presenceTracker;

        public PresenceHub(PresenceTracker presenceTracker)
        {
            this.presenceTracker = presenceTracker;
        }
        public override async Task OnConnectedAsync()
        {
            var isOnline = await presenceTracker.UserConnected(Context.User.GetUserName(), Context.ConnectionId);
            if (isOnline)
                await Clients.Others.SendAsync("UserIsOnline", Context.User.GetUserName());

            var users = await presenceTracker.GetOnlineUsers();
            await Clients.Caller.SendAsync("GetOnlineUsers", users);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var isOffline = await presenceTracker.UserDisConnected(Context.User.GetUserName(), Context.ConnectionId);
            if (isOffline)
                await Clients.Others.SendAsync("UserIsOffline", Context.User.GetUserName());
            await base.OnDisconnectedAsync(exception);
        }
    }
}