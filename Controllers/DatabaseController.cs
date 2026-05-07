using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using BAOCAO_369.Services;

namespace BAOCAO_369.Controllers
{
    public class DatabaseController : Controller
    {
        private readonly DatabaseService _dbService;

        public DatabaseController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet]
        public IActionResult ImportSql()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportSql(IFormFile sqlFile)
        {
            if (sqlFile == null || sqlFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn một file SQL để tải lên.";
                return View();
            }

            var ext = Path.GetExtension(sqlFile.FileName).ToLowerInvariant();
            if (ext != ".sql" && ext != ".csv" && ext != ".txt")
            {
                TempData["ErrorMessage"] = "Chỉ chấp nhận file có định dạng .sql, .csv hoặc .txt";
                return View();
            }

            try
            {
                using (var reader = new StreamReader(sqlFile.OpenReadStream()))
                {
                    string sqlContent = await reader.ReadToEndAsync();
                    var result = await _dbService.ImportDataAsync(sqlContent);

                    if (result.errorCount > 0)
                    {
                        TempData["WarningMessage"] = $"Đã thực thi thành công {result.successCount} lệnh. Có {result.errorCount} lệnh bị lỗi.";
                        // Ghi log hoặc hiển thị lỗi đầu tiên để user biết
                        TempData["ErrorDetails"] = result.errors.Count > 0 ? result.errors[0] : "";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"Thực thi thành công toàn bộ {result.successCount} lệnh từ file SQL. Các dữ liệu cũ đã được cập nhật disable = 1.";
                    }
                }
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi đọc hoặc xử lý file: " + ex.Message;
            }

            return View();
        }
    }
}
