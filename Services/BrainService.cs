using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using BAOCAO_369.Plugins;

namespace BAOCAO_369.Services
{
    public class BrainService
    {
        private readonly Kernel _kernel;
        private readonly IWebHostEnvironment _env;

        public BrainService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _env = env;
            // 1. Lấy thông tin cấu hình AI
            var apiKey = configuration["AI:OpenAI:ApiKey"] ?? string.Empty;
            var modelId = configuration["AI:OpenAI:ModelId"] ?? "gpt-4o";

            // Khởi tạo Kernel Builder
            var builder = Kernel.CreateBuilder();

            if (!string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_OPENAI_API_KEY") 
            {
                 builder.AddOpenAIChatCompletion(modelId, apiKey);
            }

            // 2. Đăng ký Plugin vào Kernel (Cánh tay tương tác với Database bằng SQL Thật)
            builder.Plugins.AddFromObject(new OracleReportingPlugin(configuration), "OracleReportingPlugin");

            _kernel = builder.Build();
        }

        public async Task<string> ProcessChatAsync(string userMessage)
        {
            try 
            {
                 // 3. Cơ chế Planning / Function Calling (Tự động invoke hàm C# nếu cần)
                 var settings = new OpenAIPromptExecutionSettings
                 {
                     ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                 };

                 // 4. Memory / RAG (Gắn kèm thông tin Schema từ System Prompt)
                 string schemaJson = "";
                 string filePath = Path.Combine(_env.ContentRootPath, "Data", "db_schema.json");
                 if (File.Exists(filePath))
                 {
                     schemaJson = await File.ReadAllTextAsync(filePath);
                 }

                 var systemPrompt = $"Bạn là AI Assistant nội bộ chuyên trực quan và trả lời câu hỏi về báo cáo EVN.\nĐây là cấu trúc Database Schema hiện tại dưới dạng JSON:\n{schemaJson}\nDựa vào đây và Plugin được cung cấp để truy vấn dữ liệu báo cáo giúp người dùng.";

                 var chatParams = new KernelArguments(settings);
                 
                 var result = await _kernel.InvokePromptAsync($"{systemPrompt}\nUser: {userMessage}", chatParams);
                 return result?.ToString() ?? "Không có câu trả lời.";
            } 
            catch (Exception ex) 
            {
                 return $"[Hệ thống chưa kết nối LLM] Vui lòng thiết lập OpenAI API Key trong appsettings.json. Lỗi: {ex.Message}";
            }
        }
    }
}
