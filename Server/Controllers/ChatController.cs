using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Hubs;
using Server.Interfaces;
using Server.Models;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Ensure the user is authenticated
    // The ChatController is responsible for handling chat-related operations such as sending messages, creating conversations, and managing group chats.
    public class ChatController : ControllerBase
    {
        private readonly DatabaseContext _context;
        
        //Hub
        private readonly IHubContext<ChatHub> _hub;
        //The ChatController is responsible for handling chat-related operations such as sending messages, creating conversations, and managing group chats.
        private readonly IMessageService _messageService;

        public ChatController(DatabaseContext context, IHubContext<ChatHub> hub, IMessageService messageService)
        { 
            _context = context;
            _hub = hub;
            _messageService = messageService;

        }
        
        private int GetCurrentUserId()
        {
            // Get the current user's ID from the claims
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            throw new UnauthorizedAccessException("User not authenticated");
            return int.Parse(currentUserId);
        }
        
        [HttpPost("private")]
        public async Task<IActionResult> SendPrivateMessage([FromBody]  int targetUserId)   
        {
            //Check current user is authenticated 
            var currentUserId = GetCurrentUserId();
            //ClaimTypes.NameIdentifier is used to get the user ID from the claims
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            
            // Check if target user exists
            var targetUser = await _context.Users.FindAsync(targetUserId);
            if (targetUser == null)
            {
                // If target user does not exist, return NotFound
                return NotFound(new { message = "Target user not found" });
            }
            
            //Cannot create a conversation with yourself
            if (targetUserId == currentUserId)
            {
                return BadRequest(new { message = "Cannot create a conversation with yourself" });
            }
            // Create a new conversation for private chat
            //Check if conversation already exists
            var existingConversation = await _context.Conversations
                .Where(c => !c.IsGroup)
                .Where(c => c.Participants.Count == 2)
                .Where(c => c.Participants.Any(p => p.UserId == currentUserId))
                .Where(c => c.Participants.Any(p => p.UserId == targetUserId))
                .Include(c => c.Participants)
                .FirstOrDefaultAsync();

            
            if (existingConversation == null)
            {
                return Ok(existingConversation); // Return existing conversation if found without creating a new one
            }
            // Create a new conversation

            var conversation = new Conversation {IsGroup = false}; 
            // Set IsGroup to false for private chat and create a new conversation with 2 participants (current user and target user)
            _context.Conversations.Add(conversation);
            // Add participants to the conversation
            _context.UserConversations.Add(new UserConversation
            {
                UserId = currentUserId,
                ConversationId = conversation.Id
            });
            _context.UserConversations.Add(new UserConversation
            {
                UserId = targetUserId,
                ConversationId = conversation.Id
            });
            // Save changes to the database
            await _context.SaveChangesAsync();
            // Return the created conversation
            return Ok(conversation);
        }
            
        [HttpPost("group")]

        public async Task<IActionResult> CreateGroupChat([FromBody] CreateGroupDTO dto)
{
    var currentUserId = GetCurrentUserId();

    // Validate group name
    if (string.IsNullOrWhiteSpace(dto.GroupName) || dto.GroupName.Length > 100)
        return BadRequest("Invalid group name. It must be between 1 and 100 characters.");

    // Validate user list
    if (dto.UserIds == null || dto.UserIds.Count == 0)
        return BadRequest("UserIds cannot be null or empty.");

    var userIds = dto.UserIds.Distinct().ToList(); 
    // Distinct(): to remove duplicates (ensures unique user IDs)
    if (!userIds.Contains(currentUserId)) userIds.Add(currentUserId); 
    // Ensure the current user is included in the group chat

    // Validate that all user IDs exist in the database
    // Select the UserID from the Users table where the UserID is in the userIds list
    var exists = await _context.Users
        .Where(u => userIds.Contains(u.UserID))
        .Select(u => u.UserID)
        .ToListAsync();

    var invalid = userIds.Except(exists).ToList(); // Except(): to find the invalid user IDs
    if (invalid.Any())
        return BadRequest($"Users are not exist: {string.Join(",", invalid)}");

    // Strict validation: check for existing group with same name and exact users
    var possibleGroups = await _context.Conversations
        .Where(c => c.IsGroup && c.GroupName == dto.GroupName)
        .Include(c => c.Participants)
        .ToListAsync();

    foreach (var group in possibleGroups)
    {
        var groupUserIds = group.Participants.Select(p => p.UserId).OrderBy(x => x).ToList();
        var inputUserIds = userIds.OrderBy(x => x).ToList();

        // Check for exact same members
        if (groupUserIds.SequenceEqual(inputUserIds))
        {
            return Ok(new
            {
                Message = "Group already exists with the same name and users.",
                ConversationId = group.Id
            });
        }
    }

    // Create new group
    var conv = new Conversation
    {
        IsGroup = true,
        GroupName = dto.GroupName,
        CreatedAt = DateTime.UtcNow // Optional: track creation time
    };

    _context.Conversations.Add(conv);

    foreach (var id in userIds)
    {
        _context.UserConversations.Add(new UserConversation
        {
            UserId = id,
            Conversation = conv
        });
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
        Message = "Group chat created successfully.",
        ConversationId = conv.Id,
        Users = userIds
    });
}

        [HttpPost("leave")]
        public async Task<IActionResult> LeaveGroup([FromBody] LeaveGroupDTO dto)
        {
            var userId = GetCurrentUserId();
            //uc = UserConversation which is a join table between User and Conversation
            var uc = await _context.UserConversations.FirstOrDefaultAsync(x => x.UserId == userId && x.ConversationId == dto.ConversationId);

            if (uc == null) return BadRequest("You are not a member of this group or the group does not exist.");

            _context.UserConversations.Remove(uc);
            await _context.SaveChangesAsync();

            
            // Check if there are any remaining participants in the conversation
            //If there are no remaining participants, delete the conversation
            var remaining = await _context.UserConversations.AnyAsync(x => x.ConversationId == dto.ConversationId);
            if (!remaining)
            {
                var conv = await _context.Conversations.FindAsync(dto.ConversationId);
                _context.Conversations.Remove(conv);
                await _context.SaveChangesAsync();
            }

            return Ok("You have left the group successfully.");
        }
        
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDTO dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var msg = await _messageService.SendMessageAsync(userId, dto);
                return Ok(msg);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "System Error: " + ex.Message);
            }
        }
        
        [HttpGet("history")]
        public async Task<IActionResult> GetMessages(string conversationId, int page = 1, int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                return BadRequest("ConversationId is required.");

            var userId = GetCurrentUserId();
            var isMember = await _context.UserConversations
                .AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId);

            if (!isMember) return Forbid();

            // Fetch messages for the conversation with pagination
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            // Reverse the order to show the latest messages first
            messages.Reverse();
            // Return the messages
            if (messages.Count == 0)
            {
                return NotFound("No messages found for this conversation.");
            }
            
            return Ok(messages);
        }
    }
    
}