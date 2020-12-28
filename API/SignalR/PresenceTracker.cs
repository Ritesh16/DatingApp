using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.SignalR
{
    public class PresenceTracker
    {
        private static readonly Dictionary<string, List<string>> OnlineUsers =
                new Dictionary<string, List<string>>();

        public Task<bool> UserConnected(string userName, string connectionId)
        {
            bool isOnline = false;
            lock (OnlineUsers)
            {
                if (OnlineUsers.ContainsKey(userName))
                {
                    OnlineUsers[userName].Add(connectionId);
                }
                else
                {
                    OnlineUsers.Add(userName, new List<string> { connectionId });
                    isOnline = true;
                }
            }

            return Task.FromResult(isOnline);
        }

        public Task<bool> UserDisConnected(string userName, string connectionId)
        {
            bool isOffline = false;
            lock (OnlineUsers)
            {
                if (!OnlineUsers.ContainsKey(userName)) return Task.FromResult(isOffline);

                OnlineUsers[userName].Remove(connectionId);
                if (OnlineUsers[userName].Count == 0)
                {
                    OnlineUsers.Remove(userName);
                    isOffline = true;
                }
            }

            return Task.FromResult(isOffline);
        }

        public Task<string[]> GetOnlineUsers()
        {
            string[] onlineUsers;
            lock (OnlineUsers)
            {
                onlineUsers = OnlineUsers.OrderBy(x => x.Key).Select(x => x.Key).ToArray();
            }

            return Task.FromResult(onlineUsers);
        }

        public Task<List<string>> GetConnectionsForUser(string userName)
        {
            List<string> connectionIds;
            lock(OnlineUsers)
            {
                connectionIds = OnlineUsers.GetValueOrDefault(userName);
            }

            return Task.FromResult(connectionIds);
        }
    }
}