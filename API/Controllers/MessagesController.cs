using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUserRepository userRepository;
        private readonly IMessageRepository messageRepository;
        private readonly IMapper mapper;

        public MessagesController(IUserRepository userRepository, IMessageRepository messageRepository,
                    IMapper mapper)
        {
            this.userRepository = userRepository;
            this.messageRepository = messageRepository;
            this.mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var userName = User.GetUserName();

            if (userName == createMessageDto.RecipientUserName.ToLower())
                return BadRequest("You cannot send message to yourself!");

            var sender = await userRepository.GetUserByUserNameAsync(userName);
            var recipient = await userRepository.GetUserByUserNameAsync(createMessageDto.RecipientUserName);

            if (recipient == null) return NotFound();

            var message = new Message()
            {
                Content = createMessageDto.Content,
                Recipient = recipient,
                Sender = sender,
                SendUserName = sender.UserName,
                RecipientUserName = recipient.UserName
            };

            messageRepository.AddMessage(message);

            if (await messageRepository.SaveAllAsync()) return Ok(mapper.Map<MessageDto>(message));

            return BadRequest("Failed to add message.");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageForUser([FromQuery]
            MessageParams messageParams)
        {
            messageParams.UserName = User.GetUserName();
            var messages = await messageRepository.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize,
                        messages.TotalCount, messages.TotalPages);

            return messages;
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string userName)
        {
            var currentUserName = User.GetUserName();

            return Ok(await messageRepository.GetMessagesThread(currentUserName, userName));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var userName = User.GetUserName();

            var message = await messageRepository.GetMessage(id);
            if (message.Sender.UserName != userName && message.Recipient.UserName != userName) return Unauthorized();

            if (message.Sender.UserName == userName) message.SenderDeleted = true;

            if (message.Recipient.UserName == userName) message.RecipientDeleted = true;

            if(message.RecipientDeleted && message.SenderDeleted) 
                messageRepository.DeleteMessage(message);

            if(await messageRepository.SaveAllAsync()) return Ok();
            
            return BadRequest("Problem in deleting the message");
        }
    }
}