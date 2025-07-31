using System.Threading.Tasks;

namespace PawVerse.Services.Interfaces
{
    public interface IChatbotService
    {
        /// <summary>
        /// Gửi tin nhắn đến COZE API và nhận phản hồi (stateless)
        /// </summary>
        /// <param name="userMessage">Tin nhắn của người dùng</param>
        /// <returns>Phản hồi từ chatbot</returns>
        Task<string> SendMessageAsync(string userMessage);
    }
}
