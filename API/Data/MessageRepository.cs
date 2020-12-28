using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext context;
        private readonly IMapper mapper;

        public MessageRepository(DataContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        public void AddGroup(Group group)
        {
            context.Groups.Add(group);
        }

        public void AddMessage(Message message)
        {
            context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            context.Messages.Remove(message);
        }

        public async Task<Connection> GetConnection(string connectionId)
        {
            return await context.Connections.FindAsync(connectionId);
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await context.Groups
                    .Include(c => c.Connections)
                    .Where(c => c.Connections.Any(c => c.ConnectionId == connectionId))
                    .FirstOrDefaultAsync();
        }

        public async Task<Message> GetMessage(int id)
        {
            return await context.Messages
                        .Include(x => x.Sender)
                        .Include(x => x.Recipient)
                        .SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await context.Groups
                            .Include(x => x.Connections)
                            .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = context.Messages
                        .OrderByDescending(m => m.MessageSent)
                        .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(x => x.Recipient.UserName == messageParams.UserName
                                       && x.RecipientDeleted == false),
                "Outbox" => query.Where(x => x.Sender.UserName == messageParams.UserName
                                       && x.SenderDeleted == false),
                _ => query.Where(x => x.Recipient.UserName == messageParams.UserName
                                        && x.RecipientDeleted == false
                                        && x.DateRead == null)
            };

            var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);

            return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesThread(string currentUserName, string recipientUserName)
        {
            var messages = await context.Messages
                    .Include(u => u.Sender).ThenInclude(p => p.Photos)
                    .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                    .Where(m => m.Recipient.UserName == currentUserName
                            && m.RecipientDeleted == false
                            && m.Sender.UserName == recipientUserName
                            || m.Recipient.UserName == recipientUserName
                             && m.Sender.UserName == currentUserName
                             && m.SenderDeleted == false
                    )
                    .OrderBy(m => m.MessageSent)
                    .ToListAsync();

            var unreadMessages = messages.Where(x => x.DateRead == null &&
                             x.Recipient.UserName == currentUserName).ToList();

            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }

                await context.SaveChangesAsync();
            }

            return mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public void RemoveConnection(Connection connection)
        {
            context.Connections.Remove(connection);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }
    }
}