﻿using FinalProject.DAL;
using FinalProject.DTOs;
using FinalProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinalProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly ApiDbContext _db;
        private readonly UserManager<ApiUser> _userManager;

        public MessageController(ApiDbContext db, UserManager<ApiUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateMessage([FromBody] MessageDTO dto)
        {
            if (string.IsNullOrEmpty(dto.Text)) return BadRequest();
            if (dto.PrivateChatId == null && dto.GroupChatId == null) return BadRequest("Choose chat");
            var userEmail = this.User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (dto.UserId != user.Id) return BadRequest();
            Message newMessage = new Message()
            {
                Text = dto.Text,
                Created = DateTime.Now,
                UserId = user.Id,
                PrivateChatId = dto.PrivateChatId,
                GroupChatId = dto.GroupChatId,
                isRead = dto.isRead
            };
            await _db.Messages.AddAsync(newMessage);
            await _db.SaveChangesAsync();
            return Ok();
        }
        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] int messageId)
        {
            Message messageToDelete = await _db.Messages.FirstOrDefaultAsync(x => x.Id == messageId);
            if (messageToDelete == null) return NotFound("Chat not found!");
            _db.Messages.Remove(messageToDelete);
            await _db.SaveChangesAsync();
            return Ok("Message deleted!");
        }
        [HttpGet("getChatMessages")]
        public async Task<IActionResult> GetChatMessages([FromQuery] int? chatId)
        {
            if (chatId == null) return Ok();
            var privateChat = await _db.PrivateChats.FirstOrDefaultAsync(x => x.Id == chatId);
            if (privateChat == null) return NotFound();
            List<Message> messages = await _db.Messages
                .Include(x=> x.User)
                .Where(x => x.PrivateChatId == privateChat.Id)
                .OrderBy(x => x.Created)
                .ToListAsync();
            foreach(var item in messages)
            {
                if (item.User.ImageUrl != null && !item.User.ImageUrl.Contains(@"Resources\Images\"))
                {
                    item.User.ImageUrl = @"Resources\Images\" + item.User.ImageUrl;
                }
            }
            return Ok(messages);
        }
        [HttpGet("getGroupMessages")]
        public async Task<IActionResult> GetGroupMessages([FromQuery] int? chatId)
        {
            if (chatId == null) return Ok();
            var groupChat = await _db.GroupChats.FirstOrDefaultAsync(x => x.Id == chatId);
            if (groupChat == null) return NotFound();
            List<Message> messages = await _db.Messages
                .Include(x => x.User)
                .Where(x => x.GroupChatId == groupChat.Id)
                .OrderBy(x => x.Created)
                .ToListAsync();
            foreach (var item in messages)
            {
                if (item.User.ImageUrl != null && !item.User.ImageUrl.Contains(@"Resources\Images\"))
                {
                    item.User.ImageUrl = @"Resources\Images\" + item.User.ImageUrl;
                }
            }
            return Ok(messages);
        }
        [HttpPost("isRead")]
        public async Task<IActionResult> IsRead([FromBody] int messageId)
        {
            Message messageRead = await _db.Messages.FirstOrDefaultAsync(x => x.Id == messageId);
            if (messageRead == null) return NotFound("Chat not found!");
            messageRead.isRead = true;
            _db.Messages.Update(messageRead);
            await _db.SaveChangesAsync();
            return Ok("Message is read!");
        }
        [HttpPost("areRead")]
        public async Task<IActionResult> areRead([FromBody] MessagesReadDTO dto)
        {
            if (dto.PrivateChatId == null && dto.GroupChatId == null) return BadRequest();
            //var chat = await _db.PrivateChats.FirstOrDefaultAsync(x => x.Id == dto.PrivateChatId);
            //var group = await _db.GroupChats.FirstOrDefaultAsync(x => x.Id == dto.GroupChatId);
            //if (chat == null && group == null) return NotFound("Chat is not found");
            ApiUser user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) return NotFound("User not found!");
            //var privateChatUser = await _db.PrivateChats.FirstOrDefaultAsync(x => x.Id == chat.Id && (x.UserOne.Id == user.Id || x.UserTwo.Id == user.Id));
            //var groupChatUser = await _db.GroupChatToUser.FirstOrDefaultAsync(x => x.GroupChatId == group.Id && x.UserId == user.Id);
            //if (privateChatUser == null && groupChatUser == null) return NotFound("User is not member of the chat");
            var messagesToRead = _db.Messages.Where(x => (x.PrivateChatId == dto.PrivateChatId || x.GroupChatId == dto.GroupChatId) && x.UserId != user.Id);
            if(messagesToRead != null)
            {
                foreach(var item in messagesToRead)
                {
                    item.isRead = true;
                    _db.Messages.Update(item);
                }
            }
            await _db.SaveChangesAsync();
            return Ok("Messages are read");
        }
    }
}
