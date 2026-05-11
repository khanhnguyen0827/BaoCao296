using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public IActionResult ImportSql(decimal? idDv)
        {
            ViewBag.TargetIdDv = idDv;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportSql(List<IFormFile> sqlFiles, decimal? idDv)
        {
            if (sqlFiles == null || sqlFiles.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một file để tải lên.";
                return View();
            }

            int totalSuccess = 0;
            int totalError = 0;
            var allErrors = new List<string>();

            foreach (var file in sqlFiles)
            {
                if (file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                try
                {
                    if (ext == ".xlsx")
                    {
                        var result = await _dbService.ImportExcelDataAsync(file.OpenReadStream(), idDv);
                        totalSuccess += result.successCount;
                        totalError += result.errorCount;
                        if (result.errors != null && result.errors.Count > 0) 
                            allErrors.AddRange(result.errors.Select(e => $"[{file.FileName}] {e}"));
                    }
                    else if (ext == ".sql" || ext == ".csv" || ext == ".txt")
                    {
                        using (var reader = new StreamReader(file.OpenReadStream()))
                        {
                            string sqlContent = await reader.ReadToEndAsync();
                            var result = await _dbService.ImportDataAsync(sqlContent);
                            totalSuccess += result.successCount;
                            totalError += result.errorCount;
                            if (result.errors != null && result.errors.Count > 0) 
                                allErrors.AddRange(result.errors.Select(e => $"[{file.FileName}] {e}"));
                        }
                    }
                    else
                    {
                        allErrors.Add($"[{file.FileName}] Định dạng không hỗ trợ.");
                        totalError++;
                    }
                }
                catch (System.Exception ex)
                {
                    allErrors.Add($"[{file.FileName}] Lỗi hệ thống: {ex.Message}");
                    totalError++;
                }
            }

            if (totalError > 0)
            {
                TempData["WarningMessage"] = $"Đã xử lý {sqlFiles.Count} file. Thành công: {totalSuccess}. Lỗi: {totalError}.";
                TempData["ErrorDetails"] = allErrors.Count > 0 ? string.Join("<br/>", allErrors.Take(10)) : "";
            }
            else
            {
                TempData["SuccessMessage"] = $"Nhập thành công toàn bộ {totalSuccess} bản ghi từ {sqlFiles.Count} file.";
            }

            ViewBag.TargetIdDv = idDv;
            return View();
        }
    }
}
