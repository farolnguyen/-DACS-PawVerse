using PawVerse.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawVerse.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatbotService> _logger;
        private readonly string _cozeApiKey;
        private readonly string _cozeBotId;
        private readonly string _cozeApiEndpoint;
        
        // Hằng số cấu hình cho xử lý SSE stream
        private const int MAX_RETRY_ATTEMPTS = 5; // Số lần retry tối đa
        private const int RETRY_DELAY_MS = 800; // Thời gian chờ giữa các lần retry (milliseconds)
        private const int RESPONSE_TIMEOUT_MS = 30000; // Tổng thời gian chờ tối đa cho phản hồi stream (30 giây)

        public ChatbotService(HttpClient httpClient, IConfiguration configuration, ILogger<ChatbotService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _cozeApiKey = _configuration["Coze:ApiKey"];
            _cozeBotId = _configuration["Coze:BotId"];
            _cozeApiEndpoint = _configuration["Coze:ApiEndpoint"];
            
            _logger.LogInformation($"Coze API Configuration: BotId={_cozeBotId}, Endpoint={_cozeApiEndpoint}");
            
            // Thiết lập header cho HTTP client
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _cozeApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Lọc và làm sạch nội dung phản hồi từ Assistant, loại bỏ các thông tin nội bộ
        /// </summary>
        /// <param name="response">Chuỗi JSON hoặc văn bản từ chatbot</param>
        /// <returns>Chỉ nội dung phản hồi đã lọc</returns>
        private string CleanAssistantResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return string.Empty;

            try
            {
                _logger.LogInformation($"Cleaning response: {response.Substring(0, Math.Min(100, response.Length))}...");
                
                // Pattern 0: Nếu response bắt đầu với {"msg_type":"knowledge_recall"}, lấy phần sau JSON này nếu có
                if (response.StartsWith("{\"msg_type\":\"knowledge_recall\""))
                {
                    _logger.LogInformation("Detected direct knowledge_recall message, finding actual answer");
                    
                    // Tìm phần sau JSON knowledge_recall
                    // Tìm vị trí kết thúc của JSON knowledge_recall đầu tiên
                    int endPos = -1;
                    int depth = 0;
                    bool inString = false;
                    
                    for (int i = 0; i < response.Length; i++)
                    {
                        char c = response[i];
                        
                        if (c == '\"' && (i == 0 || response[i - 1] != '\\'))
                            inString = !inString;
                            
                        if (!inString)
                        {
                            if (c == '{')
                                depth++;
                            else if (c == '}')
                            {
                                depth--;
                                if (depth == 0)
                                {
                                    endPos = i;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (endPos > 0 && endPos + 1 < response.Length)
                    {
                        string remainingText = response.Substring(endPos + 1).Trim();
                        
                        // Nếu có nội dung hữu ích
                        if (!string.IsNullOrEmpty(remainingText) && 
                            !remainingText.StartsWith("{\"msg_type\":\"generate_answer_finish\""))
                        {
                            _logger.LogInformation($"Found content after knowledge_recall: {remainingText.Substring(0, Math.Min(50, remainingText.Length))}...");
                            return remainingText;
                        }
                        else if (remainingText.StartsWith("{\"msg_type\":\"generate_answer_finish\"")) 
                        {
                            // Tìm xem có nội dung ở giữa không
                            int startOfGenerateFinish = response.IndexOf("{\"msg_type\":\"generate_answer_finish\"");
                            if (startOfGenerateFinish > endPos + 1)
                            {
                                string middleContent = response.Substring(endPos + 1, startOfGenerateFinish - endPos - 1).Trim();
                                if (!string.IsNullOrEmpty(middleContent))
                                {
                                    _logger.LogInformation($"Found content between knowledge_recall and generate_answer_finish: {middleContent}");
                                    return CleanDuplicatedContent(middleContent);
                                }
                            }
                        }
                    }
                    
                    // Nếu không tìm thấy nội dung rõ ràng sau knowledge_recall, thử phân tích JSON
                    try
                    {
                        string jsonPart = response.Substring(0, endPos + 1);
                        var jsonDoc = JsonDocument.Parse(jsonPart);
                        var root = jsonDoc.RootElement;
                        
                        if (root.TryGetProperty("data", out var dataElement))
                        {
                            // Đây là trường hợp hay gặp, data có thể chứa dữ liệu có thể sử dụng
                            string dataStr = dataElement.GetString();
                            if (!string.IsNullOrEmpty(dataStr))
                            {
                                // Thử phân tích data như một JSON object nếu có thể
                                try
                                {
                                    var dataJson = JsonDocument.Parse(dataStr);
                                    // Trong một số trường hợp, data sẽ chứa các chunked content từ knowledge base
                                    // Ta có thể bỏ qua dữ liệu này vì đây là thông tin nội bộ
                                    _logger.LogInformation("Data field contains valid JSON, but likely internal data. Searching for answer elsewhere.");
                                }
                                catch
                                {
                                    // Nếu data không phải JSON, có thể đây là câu trả lời trực tiếp
                                    if (!dataStr.Contains("chunks") && !dataStr.Contains("slice"))
                                    {
                                        _logger.LogInformation($"Found potential answer in data field: {dataStr}");
                                        return dataStr;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error parsing knowledge_recall JSON: {ex.Message}");
                    }
                }
                
                // Pattern 1: Trích xuất nội dung giữa phần knowledge_recall và generate_answer_finish
                if (response.Contains("knowledge_recall") && response.Contains("generate_answer_finish"))
                {
                    // Phân tích cấu trúc: JSON knowledge_recall + phần text trả lời + JSON generate_answer_finish
                    _logger.LogInformation("Detected pattern: knowledge_recall + answer + generate_answer_finish");
                    
                    // Phương pháp 1: Tìm vị trí của phần kết thúc dữ liệu knowledge_recall và bắt đầu generate_answer_finish
                    int knowledgeEndPos = response.IndexOf("}", response.IndexOf("knowledge_recall"));
                    int depth = 1;
                    bool inString = false;
                    
                    // Tìm dấu } tương ứng với JSON knowledge_recall
                    for (int i = response.IndexOf("{", response.IndexOf("knowledge_recall")) + 1; i < response.Length; i++)
                    {
                        char c = response[i];
                        
                        if (c == '\"' && (i == 0 || response[i - 1] != '\\'))
                            inString = !inString;
                            
                        if (!inString)
                        {
                            if (c == '{')
                                depth++;
                            else if (c == '}')
                            {
                                depth--;
                                if (depth == 0)
                                {
                                    knowledgeEndPos = i;
                                    break;
                                }
                            }
                        }
                    }
                    
                    int answerFinishPos = response.IndexOf("{\"msg_type\":\"generate_answer_finish\"");
                    if (knowledgeEndPos > 0 && answerFinishPos > knowledgeEndPos)
                    {
                        // Trích xuất phần nội dung ở giữa
                        string cleanedText = response.Substring(knowledgeEndPos + 1, answerFinishPos - knowledgeEndPos - 1).Trim();
                        _logger.LogInformation($"Extracted content between knowledge_recall and generate_answer_finish: {cleanedText}");
                        
                        return CleanDuplicatedContent(cleanedText);
                    }
                }
                
                // Pattern 2: Phản hồi có thể là JSON với trường "content" hoặc "answer" 
                if (response.StartsWith("{") && response.EndsWith("}")
)
                {
                    _logger.LogInformation("Detected pattern: JSON object");
                    return ExtractAnswerFromJson(response);
                }
                
                // Pattern 3: Trích xuất nội dung giữa dấu ngoặc kép (khi câu trả lời là một chuỗi)
                if (response.Contains("\"") && !response.StartsWith("{\"msg_type\""))
                {
                    _logger.LogInformation("Detected pattern: quoted string");
                    int startQuote = response.IndexOf("\"");
                    int endQuote = response.LastIndexOf("\"");
                    if (startQuote >= 0 && endQuote > startQuote)
                    {
                        return response.Substring(startQuote + 1, endQuote - startQuote - 1);
                    }
                }
                
                // Pattern 4: Nếu chứa knowledge_recall, cắt bỏ phần đó
                if (response.Contains("knowledge_recall"))
                {
                    _logger.LogInformation("Detected pattern: contains knowledge_recall");
                    // Tìm vị trí bắt đầu của knowledge_recall
                    int knowledgeStartPos = response.IndexOf("knowledge_recall");
                    if (knowledgeStartPos > 0)
                    {
                        // Tìm trước đó có nội dung hữu ích không
                        string beforeKnowledge = response.Substring(0, knowledgeStartPos).Trim();
                        if (!string.IsNullOrEmpty(beforeKnowledge) && 
                            !beforeKnowledge.EndsWith(":") && 
                            !beforeKnowledge.EndsWith(",") && 
                            !beforeKnowledge.EndsWith("{"))
                        {
                            return beforeKnowledge;
                        }
                        
                        // Tìm phần sau knowledge_recall
                        int endBrace = response.IndexOf("}", knowledgeStartPos);
                        if (endBrace > 0 && endBrace + 1 < response.Length)
                        {
                            string afterKnowledge = response.Substring(endBrace + 1).Trim();
                            if (!string.IsNullOrEmpty(afterKnowledge) && !afterKnowledge.StartsWith("{\"msg_type\""))
                            {
                                return afterKnowledge;
                            }
                        }
                    }
                }
                
                // Trường hợp không khớp với các pattern trên
                _logger.LogInformation("No specific pattern matched, returning original");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning response: {ex.Message}");
                return response; // Trả về chuỗi gốc trong trường hợp lỗi
            }
        }
        
        /// <summary>
        /// Xử lý nội dung bị trùng lặp trong phản hồi
        /// </summary>
        private string CleanDuplicatedContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
                
            // Loại bỏ các phần lặp lại (nếu có)
            if (content.Length > 60)
            {
                // Kiểm tra nếu nội dung giống nhau xuất hiện 2 lần liên tiếp
                int halfLength = content.Length / 2;
                string firstHalf = content.Substring(0, halfLength).Trim();
                string secondHalf = content.Substring(halfLength).Trim();
                
                // Nếu nội dung bị lặp lại hoàn toàn
                if (firstHalf.Equals(secondHalf))
                {
                    _logger.LogInformation("Found exact content duplication, returning first half");
                    return firstHalf;
                }
                
                // Kiểm tra nếu phần đầu của firstHalf xuất hiện trong secondHalf
                int firstPortion = Math.Min(40, firstHalf.Length);
                if (firstHalf.Length > firstPortion && secondHalf.Contains(firstHalf.Substring(0, firstPortion)))
                {
                    _logger.LogInformation("Found partial content duplication, returning first half");
                    return firstHalf;
                }
                
                // Kiểm tra nếu phần cuối của firstHalf xuất hiện ở đầu secondHalf
                int lastPortion = Math.Min(40, firstHalf.Length);
                if (firstHalf.Length > lastPortion && 
                    secondHalf.StartsWith(firstHalf.Substring(firstHalf.Length - lastPortion)))
                {
                    _logger.LogInformation("Found overlapping content duplication, removing overlap");
                    return firstHalf + secondHalf.Substring(lastPortion);
                }
                
                // Tìm kiếm và loại bỏ cụm từ "lặp lại nội dung" nếu có
                if (content.Contains("lặp lại nội dung") || 
                    content.Contains("Lặp lại nội dung") ||
                    content.Contains("xin lặp lại") || 
                    content.Contains("Xin lặp lại"))
                {
                    // Tìm vị trí đầu tiên xuất hiện
                    int repeatPos = content.IndexOf("lặp lại", StringComparison.OrdinalIgnoreCase);
                    if (repeatPos > 0)
                    {
                        _logger.LogInformation("Found 'lặp lại nội dung', truncating content");
                        return content.Substring(0, repeatPos).Trim();
                    }
                }
            }
            
            return content;
        }
        
        /// <summary>
        /// Trích xuất nội dung phản hồi từ chuỗi JSON
        /// </summary>
        private string ExtractAnswerFromJson(string jsonText)
        {
            try
            {
                // Thử phân tích json
                var jsonDoc = JsonDocument.Parse(jsonText);
                var root = jsonDoc.RootElement;
                
                // Kiểm tra các trường phổ biến của COZE API
                string[] commonFields = { "answer", "content", "message", "text", "response" };
                
                foreach (var field in commonFields)
                {
                    if (root.TryGetProperty(field, out var element))
                    {
                        string value = element.GetString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            _logger.LogInformation($"Found answer in field {field}: {value}");
                            return value;
                        }
                    }
                }
                
                // Nếu không tìm thấy trong các trường phổ biến, tìm bất kỳ trường nào có giá trị chuỗi
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        string value = property.Value.GetString();
                        if (!string.IsNullOrEmpty(value) && 
                            !property.Name.Contains("knowledge") && 
                            !property.Name.Contains("id") && 
                            !property.Name.Contains("type"))
                        {
                            _logger.LogInformation($"Found potential answer in field {property.Name}: {value}");
                            return value;
                        }
                    }
                }
                
                _logger.LogWarning("Could not extract answer from JSON, returning original");
                return jsonText;
            }
            catch
            {
                _logger.LogError("Error parsing JSON, returning original text");
                return jsonText;
            }
        }
        
        /// <summary>
        /// Kiểm tra xem chuỗi có phải là JSON hợp lệ không
        /// </summary>
        private bool IsValidJson(string json)
        {
            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> SendMessageAsync(string userMessage)
        {
            try
            {
                var requestData = new CozeSendMessageRequest
                {
                    BotId = _cozeBotId,
                    UserId = "website_user_" + Guid.NewGuid().ToString(), 
                    AdditionalMessages = new List<CozeMessage>
                    {
                        new CozeMessage
                        {
                            Role = "user",
                            Content = userMessage,
                            ContentType = "text"
                        }
                    },
                    Stream = true, // Sử dụng stream mode
                    AutoSaveHistory = false // Không lưu lịch sử trò chuyện
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    }),
                    Encoding.UTF8,
                    "application/json"
                );

                _logger.LogInformation($"Sending streaming request to COZE API: {_cozeApiEndpoint}");
                
                // Tạo request với method POST
                var request = new HttpRequestMessage(HttpMethod.Post, _cozeApiEndpoint)
                {
                    Content = content
                };
                
                // Thêm header chấp nhận text/event-stream cho SSE
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                
                // Gửi request và nhận response
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"COZE API Error: {response.StatusCode}, {errorContent}");
                    return $"Lỗi khi gọi API: {response.StatusCode}";
                }
                
                // Xử lý phản hồi stream từ COZE API
                _logger.LogInformation("Starting to read stream response from COZE API");
                
                // In ra toàn bộ response content để debug
                var rawResponseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Raw COZE response: {rawResponseContent}");
                
                // Reset lại content để đọc lại từ stream
                var newContent = new StringContent(rawResponseContent, Encoding.UTF8, "text/event-stream");
                using (var stream = await newContent.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                {
                    var assistantContent = new StringBuilder(); // Tích lũy tất cả các nội dung
                    var firstCleanContent = ""; // Biến lưu nội dung sạch đầu tiên
                    var isCompleted = false;
                    var startTime = DateTime.Now;
                    var lineCount = 0;
                    
                    // Đọc stream cho đến khi hoàn thành hoặc timeout
                    while (!reader.EndOfStream && !isCompleted && (DateTime.Now - startTime).TotalMilliseconds < RESPONSE_TIMEOUT_MS)
                    {
                        // Đọc từng dòng trong SSE stream
                        string line = await reader.ReadLineAsync();
                        lineCount++;
                        
                        if (string.IsNullOrEmpty(line))
                        {
                            _logger.LogInformation($"Empty line {lineCount} in stream");
                            continue; // Bỏ qua dòng trống
                        }
                        
                        _logger.LogInformation($"SSE Stream line {lineCount}: {line}");
                        
                        // Xử lý các sự kiện SSE
                        if (line.StartsWith("event:"))
                        {
                            string eventType = line.Substring("event:".Length).Trim();
                            _logger.LogInformation($"SSE Event Type: {eventType}");
                            
                            if (eventType == "conversation.chat.completed")
                            {
                                isCompleted = true;
                                _logger.LogInformation("Detected completion event");
                            }
                        }
                        else if (line.StartsWith("data:"))
                        {
                            string data = line.Substring("data:".Length).Trim();
                            _logger.LogInformation($"SSE Data: {data}");
                            
                            try
                            {
                                // Kiểm tra xem data có phải là JSON hợp lệ không
                                if (JsonDocument.Parse(data) is JsonDocument doc)
                                {
                                    // Xử lý phản hồi trực tiếp có "type":"answer" (định dạng mới của COZE)
                                    if (doc.RootElement.TryGetProperty("type", out var typeProperty) &&
                                        doc.RootElement.TryGetProperty("role", out var roleProperty) &&
                                        typeProperty.GetString() == "answer" &&
                                        roleProperty.GetString() == "assistant" &&
                                        doc.RootElement.TryGetProperty("content", out var contentProperty))
                                    {
                                        string chunkContent = contentProperty.GetString();
                                        _logger.LogInformation($"Found assistant answer chunk: {chunkContent}");
                                        
                                        // Tích lũy các đoạn phản hồi
                                        if (!string.IsNullOrEmpty(chunkContent))
                                        {
                                            // Khởi tạo nếu chưa có
                                            if (firstCleanContent == null)
                                            {
                                                firstCleanContent = "[DIRECT_ANSWER]";
                                                assistantContent.Clear(); // Xóa tất cả nội dung tích lũy trước đó
                                            }
                                            
                                            // Tích lũy nội dung
                                            firstCleanContent += chunkContent;
                                            assistantContent.Append(chunkContent);
                                            _logger.LogInformation($"Accumulated assistant answer: {assistantContent}");
                                            
                                            // KHÔNG đánh dấu đã hoàn thành để tiếp tục nhận các đoạn tiếp theo
                                        }
                                    }
                                    // Kiểm tra xem có phải là thông báo msg_type không
                                    else if (doc.RootElement.TryGetProperty("msg_type", out var msgType))
                                    {
                                        var msgTypeStr = msgType.GetString();
                                        _logger.LogInformation($"Found message type: {msgTypeStr}");
                                        
                                        // Lọc ra chỉ lấy phản hồi thực tế của chatbot, bỏ qua dữ liệu nội bộ
                                        if (msgTypeStr == "generate_answer" || msgTypeStr == "generate_answer_finish")
                                        {
                                            // Thông thường phản hồi sẽ nằm trong phần data
                                            if (doc.RootElement.TryGetProperty("data", out var dataProperty))
                                            {
                                                // Xử lý data dựa trên msg_type
                                                if (msgTypeStr == "generate_answer")
                                                {
                                                    // Trích xuất nội dung phản hồi trực tiếp
                                                    string actualContent = dataProperty.GetString();
                                                    
                                                    // Kiểm tra xem nội dung có phải JSON không
                                                    if (!string.IsNullOrEmpty(actualContent))
                                                    {
                                                        try
                                                        {
                                                            // Nếu là JSON, xử lý để loại bỏ knowledge_recall
                                                            if (actualContent.StartsWith("{") || actualContent.Contains("knowledge_recall"))
                                                            {
                                                                var cleanedContent = CleanAssistantResponse(actualContent);
                                                                if (!string.IsNullOrEmpty(cleanedContent))
                                                                {
                                                                    _logger.LogInformation($"Found cleaned answer: {cleanedContent}");
                                                                    
                                                                    // Ưu tiên trả về nội dung assistant đầu tiên tìm thấy (thường là nội dung sạch)
                                                                    if (string.IsNullOrEmpty(assistantContent.ToString()))
                                                                    {
                                                                        // Chỉ lưu nội dung đầu tiên tìm thấy, không tích lũy các phần sau
                                                                        assistantContent.Append(cleanedContent);
                                                                        
                                                                        // Kiểm tra xem có phải là nội dung hoàn chỉnh không
                                                                        if (!string.IsNullOrWhiteSpace(cleanedContent) && 
                                                                            !cleanedContent.Contains("knowledge_recall") && 
                                                                            !cleanedContent.Contains("generate_answer_finish"))
                                                                        {
                                                                            // Nếu tìm thấy nội dung hoàn chỉnh, đánh dấu đã hoàn thành
                                                                            _logger.LogInformation("Found complete assistant content. Stopping stream processing.");
                                                                            isCompleted = true;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // Nếu là văn bản thông thường, thêm trực tiếp
                                                                _logger.LogInformation($"Found cleaned answer: {actualContent}");
                                                                
                                                                // Tích lũy nội dung
                                                                assistantContent.Append(actualContent);
                                                                
                                                                // Lưu lại nội dung sạch đầu tiên từ assistant
                                                                if (string.IsNullOrEmpty(firstCleanContent) && 
                                                                    !string.IsNullOrWhiteSpace(actualContent) && 
                                                                    !actualContent.Contains("knowledge_recall") && 
                                                                    !actualContent.Contains("generate_answer_finish"))
                                                                {
                                                                    firstCleanContent = actualContent;
                                                                    _logger.LogInformation($"Saved first clean assistant content: {firstCleanContent}");
                                                                    
                                                                    // Nếu tìm thấy nội dung sạch đầu tiên, đánh dấu đã hoàn thành
                                                                    _logger.LogInformation("Found complete assistant content. Stopping stream processing.");
                                                                    isCompleted = true;
                                                                }
                                                            }
                                                        }
                                                        catch (Exception innerEx)
                                                        {
                                                            // Nếu xử lý lỗi, thêm nội dung gốc (đã được kiểm tra trước đó)
                                                            _logger.LogWarning($"Inner exception while parsing content: {innerEx.Message}");
                                                            if (!actualContent.Contains("knowledge_recall") && !actualContent.Contains("msg_type"))
                                                            {
                                                                _logger.LogInformation($"Adding content after parsing error: {actualContent}");
                                                                
                                                                // Tích lũy nội dung
                                                                assistantContent.Append(actualContent);
                                                                
                                                                // Lưu lại nội dung sạch đầu tiên từ assistant
                                                                if (string.IsNullOrEmpty(firstCleanContent) && 
                                                                    !string.IsNullOrWhiteSpace(actualContent) && 
                                                                    !actualContent.Contains("knowledge_recall") && 
                                                                    !actualContent.Contains("generate_answer_finish"))
                                                                {
                                                                    firstCleanContent = actualContent;
                                                                    _logger.LogInformation($"Saved first clean assistant content after parsing error: {firstCleanContent}");
                                                                    
                                                                    // Nếu tìm thấy nội dung sạch đầu tiên, đánh dấu đã hoàn thành
                                                                    _logger.LogInformation("Found complete assistant content after parsing error. Stopping stream processing.");
                                                                    isCompleted = true;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception streamEx)
                            {
                                _logger.LogWarning($"Exception parsing SSE data: {streamEx.Message}");
                            }
                        }
                    }
                    
                    // Quyết định nội dung trả về
                    if (!string.IsNullOrEmpty(firstCleanContent))
                    {
                        // Kiểm tra xem đây có phải là câu trả lời trực tiếp không
                        if (firstCleanContent.StartsWith("[DIRECT_ANSWER]"))
                        {
                            // Trả về câu trả lời trực tiếp mà không cần xử lý thêm
                            string directResult = firstCleanContent.Substring("[DIRECT_ANSWER]".Length);
                            _logger.LogInformation($"Returning direct answer content: {directResult}");
                            return directResult;
                        }
                        else
                        {
                            // Ưu tiên sử dụng nội dung sạch đầu tiên (thường là câu trả lời hoàn chỉnh)
                            string finalResult = RemoveDuplicatedContent(firstCleanContent);
                            _logger.LogInformation($"Returning first clean content: {finalResult}");
                            return finalResult;
                        }
                    }
                    else if (!string.IsNullOrEmpty(assistantContent.ToString()))
                    {
                        // Nếu không có nội dung sạch đầu tiên, sử dụng tất cả nội dung tích lũy
                        string accumulated = RemoveDuplicatedContent(assistantContent.ToString());
                        _logger.LogInformation($"Returning accumulated content: {accumulated}");
                        return accumulated;
                    }
                    else
                    {
                        // Nếu không có nội dung nào, trả về thông báo
                        _logger.LogWarning("No assistant content found in stream");
                        return "Không nhận được phản hồi từ chatbot.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception when calling COZE API: {ex}");
                return $"Đã xảy ra lỗi: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Loại bỏ nội dung lặp lại trong phản hồi của chatbot
        /// </summary>
        /// <param name="content">Nội dung cần xử lý</param>
        /// <returns>Nội dung đã được xử lý không còn lặp lại</returns>
        private string RemoveDuplicatedContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
                
            _logger.LogInformation($"Removing duplicated content from string of length: {content.Length}");
            
            // Xử lý trường hợp đặc biệt khi có hai câu trả lời trùng nhau
            int potentialDuplicateIndex = FindPotentialDuplicateStart(content);
            if (potentialDuplicateIndex > 0)
            {
                string firstPart = content.Substring(0, potentialDuplicateIndex);
                _logger.LogInformation($"Found potential duplicate at position {potentialDuplicateIndex}, returning first part");
                return firstPart;
            }
            
            // Xử lý trường hợp cụ thể khi nội dung bị lặp lại hoàn toàn (phản hồi lặp lại 2 lần giống hệt)
            // Ví dụ: "Xin chào!Xin chào!" -> "Xin chào!"
            if (content.Length % 2 == 0)
            {
                int halfLength = content.Length / 2;
                string firstHalf = content.Substring(0, halfLength);
                string secondHalf = content.Substring(halfLength);
                
                if (firstHalf == secondHalf)
                {
                    _logger.LogInformation("Detected exact duplicate content, returning first half");
                    return firstHalf;
                }
            }
            
            // Tìm và loại bỏ những câu hoàn chỉnh bị lặp lại
            // Chia nội dung thành các câu riêng biệt
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 5) // Chỉ xử lý các câu đủ dài
                .ToList();
                
            if (sentences.Count <= 1)
                return content; // Không đủ câu để kiểm tra lặp lại
                
            List<string> uniqueSentences = new List<string>();
            for (int i = 0; i < sentences.Count; i++)
            {
                string current = sentences[i];
                bool isDuplicate = false;
                
                // So sánh với các câu đã có trong danh sách uniqueSentences
                foreach (var unique in uniqueSentences.ToList())
                {
                    // Kiểm tra trùng lặp hoàn toàn hoặc chứa nhau
                    if (unique.Contains(current) || current.Contains(unique))
                    {
                        isDuplicate = true;
                        break;
                    }
                    
                    // Tính độ tương đồng giữa hai câu
                    double similarity = CalculateSimilarity(NormalizeText(unique), NormalizeText(current));
                    
                    // Nếu độ tương đồng vượt quá ngưỡng, coi là trùng lặp
                    if (similarity > 0.7) // Ngưỡng 70%
                    {
                        _logger.LogInformation($"Found similar sentences: '{unique}' and '{current}' with similarity {similarity}. Treating as duplicate.");
                        isDuplicate = true;
                        
                        // Nếu câu hiện tại dài hơn, thay thế câu cũ bằng câu mới
                        if (current.Length > unique.Length)
                        {
                            uniqueSentences.Remove(unique);
                            uniqueSentences.Add(current);
                            _logger.LogInformation($"Replaced shorter sentence with longer one");
                        }
                        
                        break;
                    }
                }
                
                // Nếu không trùng lặp, thêm vào danh sách
                if (!isDuplicate)
                {
                    uniqueSentences.Add(current);
                }
            }
            
            // Kết hợp các câu không trùng lặp
            var result = string.Join(". ", uniqueSentences) + ".";
            _logger.LogInformation($"Removed duplicates: Original had {sentences.Count} sentences, result has {uniqueSentences.Count}");
            
            return result;
        }
        
        /// <summary>
        /// Chuẩn hóa văn bản để so sánh bằng cách loại bỏ dấu cách thừa và chuyển về chữ thường
        /// </summary>
        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            // Loại bỏ dấu cách thừa và chuyển về chữ thường
            return new string(text.ToLowerInvariant()
                .Where(c => !char.IsWhiteSpace(c) && char.IsLetterOrDigit(c))
                .ToArray());
        }
        
        /// <summary>
        /// Tính độ tương đồng giữa hai chuỗi (Jaccard similarity)
        /// </summary>
        private double CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0;
                
            // Lấy các ký tự duy nhất từ mỗi chuỗi
            var set1 = new HashSet<char>(s1);
            var set2 = new HashSet<char>(s2);
            
            // Tính tổng hợp
            var union = new HashSet<char>(set1);
            union.UnionWith(set2);
            
            // Tính giao
            var intersection = new HashSet<char>(set1);
            intersection.IntersectWith(set2);
            
            // Tính độ tương đồng Jaccard
            if (union.Count == 0)
                return 0;
                
            return (double)intersection.Count / union.Count;
        }
        
        /// <summary>
        /// Tìm vị trí tiềm năng bắt đầu của phần trùng lặp trong chuỗi
        /// </summary>
        private int FindPotentialDuplicateStart(string content)
        {
            if (string.IsNullOrEmpty(content) || content.Length < 20)
                return -1;
                
            // Tím những từ hoặc cụm từ đặc trưng xuất hiện nhiều lần
            string[] keywords = new[] { "Xin chào", "Bạn có cần", "Tôi rất", "hỗ trợ bạn", "về sản phẩm" };
            
            foreach (var keyword in keywords)
            {
                int firstIndex = content.IndexOf(keyword);
                // Tìm lần xuất hiện thứ 2 của từ khóa, nếu có
                if (firstIndex >= 0)
                {
                    int secondIndex = content.IndexOf(keyword, firstIndex + keyword.Length);
                    if (secondIndex > 0)
                    {
                        // Trả về vị trí của lần xuất hiện thứ 2 (hoặc hơi sớm hơn một chút)
                        // Có thể điều chỉnh để bắt đầu cắt từ đầu câu
                        int position = Math.Max(0, secondIndex - 5);
                        
                        // Tìm vị trí đầu câu gần nhất
                        int sentenceStart = content.LastIndexOf('.', position);
                        if (sentenceStart >= 0)
                        {
                            return sentenceStart + 1; // +1 để bỏ qua dấu chấm
                        }
                        
                        return position;
                    }
                }
            }
            
            return -1;
        }
    }

    // Classes for COZE API request and response
    public class CozeSendMessageRequest
    {
        [JsonPropertyName("bot_id")]
        public string BotId { get; set; }
        
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
        
        [JsonPropertyName("additional_messages")]
        public List<CozeMessage> AdditionalMessages { get; set; }
        
        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
        
        [JsonPropertyName("auto_save_history")]
        public bool AutoSaveHistory { get; set; } = true;
        
        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; }
    }

    public class CozeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = "text";
        
        [JsonPropertyName("attachments")]
        public List<object> Attachments { get; set; }
    }

    public class CozeApiResponse
    {
        [JsonPropertyName("data")]
        public CozeResponseData Data { get; set; }
        
        [JsonPropertyName("code")]
        public int Code { get; set; }
        
        [JsonPropertyName("msg")]
        public string Message { get; set; }
    }
    
    public class CozeResponseData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; }
        
        [JsonPropertyName("bot_id")]
        public string BotId { get; set; }
        
        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; }
        
        [JsonPropertyName("messages")]
        public List<CozeMessage> Messages { get; set; }
        
        [JsonPropertyName("last_error")]
        public CozeError LastError { get; set; }
    }
    
    public class CozeError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        
        [JsonPropertyName("msg")]
        public string Message { get; set; }
    }
    
    // Cấu trúc dữ liệu cho streaming API
    public class CozeStreamChunk
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; }
        
        [JsonPropertyName("bot_id")]
        public string BotId { get; set; }
        
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }
        
        [JsonPropertyName("chat_id")]
        public string ChatId { get; set; }
        
        [JsonPropertyName("section_id")]
        public string SectionId { get; set; }
        
        [JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }
        
        [JsonPropertyName("updated_at")]
        public long? UpdatedAt { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}
