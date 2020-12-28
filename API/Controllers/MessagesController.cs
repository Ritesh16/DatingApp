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
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;

        public MessagesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var userName = User.GetUserName();

            if (userName == createMessageDto.RecipientUserName.ToLower())
                return BadRequest("You cannot send message to yourself!");

            var sender = await unitOfWork.UserRepository.GetUserByUserNameAsync(userName);
            var recipient = await unitOfWork.UserRepository.GetUserByUserNameAsync(createMessageDto.RecipientUserName);

            if (recipient == null) return NotFound();

            var message = new Message()
            {
                Content = createMessageDto.Content,
                Recipient = recipient,
                Sender = sender,
                SendUserName = sender.UserName,
                RecipientUserName = recipient.UserName
            };

            unitOfWork.MessageRepository.AddMessage(message);

            if (await unitOfWork.Complete()) return Ok(mapper.Map<MessageDto>(message));

            return BadRequest("Failed to add message.");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageForUser([FromQuery]
            MessageParams messageParams)
        {
            messageParams.UserName = User.GetUserName();
            var messages = await unitOfWork.MessageRepository.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize,
                        messages.TotalCount, messages.TotalPages);

            return messages;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var userName = User.GetUserName();

            var message = await unitOfWork.MessageRepository.GetMessage(id);
            if (message.Sender.UserName != userName && message.Recipient.UserName != userName) return Unauthorized();

            if (message.Sender.UserName == userName) message.SenderDeleted = true;

            if (message.Recipient.UserName == userName) message.RecipientDeleted = true;

            if(message.RecipientDeleted && message.SenderDeleted) 
                unitOfWork.MessageRepository.DeleteMessage(message);

            if(await unitOfWork.Complete()) return Ok();
            
            return BadRequest("Problem in deleting the message");
        }
    }
}