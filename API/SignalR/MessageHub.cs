using System;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IHubContext<PresenceHub> presenceHub;
        private readonly PresenceTracker presenceTracker;
        private readonly IMapper mapper;
        private readonly IUnitOfWork _unitOfWork;

        public MessageHub(IUnitOfWork unitOfWork, IMapper mapper,
                  IHubContext<PresenceHub> presenceHub, PresenceTracker presenceTracker)
        {
            _unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.presenceHub = presenceHub;
            this.presenceTracker = presenceTracker;
        }

        public async override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var caller = Context.User.GetUserName();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(caller, otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _unitOfWork.MessageRepository.GetMessagesThread(caller, otherUser);
            if (_unitOfWork.HasChanges()) await _unitOfWork.Complete();

            await Clients.Caller.SendAsync("ReceiverMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var userName = Context.User.GetUserName();

            if (userName == createMessageDto.RecipientUserName.ToLower())
                throw new HubException("You cannot send message to yourself!");

            var sender = await _unitOfWork.UserRepository.GetUserByUserNameAsync(userName);
            var recipient = await _unitOfWork.UserRepository.GetUserByUserNameAsync(createMessageDto.RecipientUserName);

            if (recipient == null) throw new HubException("Not Found user");

            var message = new Message()
            {
                Content = createMessageDto.Content,
                Recipient = recipient,
                Sender = sender,
                SendUserName = sender.UserName,
                RecipientUserName = recipient.UserName
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);

            var group = await _unitOfWork.MessageRepository.GetMessageGroup(groupName);
            if (group.Connections.Any(x => x.UserName == recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await presenceTracker.GetConnectionsForUser(recipient.UserName);
                if (connections != null)
                {
                    await presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
                     new { userName = sender.UserName, knownAs = sender.KnownAs });
                }
            }

            _unitOfWork.MessageRepository.AddMessage(message);

            if (await _unitOfWork.Complete())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", mapper.Map<MessageDto>(message));
            }
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await _unitOfWork.MessageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUserName());

            if (group == null)
            {
                group = new Group(groupName);
                _unitOfWork.MessageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);
            if (await _unitOfWork.Complete()) return group;

            throw new HubException("Failed to join group.");
        }

        private async Task<Group> RemoveFromMessageGroup()
        {
            var group = await _unitOfWork.MessageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _unitOfWork.MessageRepository.RemoveConnection(connection);
            if (await _unitOfWork.Complete()) return group;

            throw new HubException("Failed to remove from group");
        }
    }
}