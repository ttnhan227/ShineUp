using Server.DTOs;
using Server.Models;

namespace Server.Interfaces;

public interface IMessageService
{
    Task<Message> SendMessageAsync(int senderId, SendMessageDTO dto);
}