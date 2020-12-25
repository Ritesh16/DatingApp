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
            await presenceTracker.UserConnected(Context.User.GetUserName(),  Context.ConnectionId);
            await Clients.Others.SendAsync("UserIsOnline", Context.User.GetUserName());

            var users = await presenceTracker.GetOnlineUsers();
            await Clients.All.SendAsync("GetOnlineUsers", users);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await presenceTracker.UserDisConnected(Context.User.GetUserName(), Context.ConnectionId);
            await Clients.Others.SendAsync("UserIsOffline", Context.User.GetUserName());

            var users = await presenceTracker.GetOnlineUsers();
            await Clients.All.SendAsync("GetOnlineUsers", users);
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}