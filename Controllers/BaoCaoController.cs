// Import các thư viện hệ thống cơ bản cần thiết
using System;
using System.Collections.Generic;
using System.Linq;
// Import thư viện ASP.NET Core MVC để xây dựng Controller và các action
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
// Import không gian tên chứa các Services xử lý nghiệp vụ của ứng dụng
using BAOCAO_369.Services;
// Import không gian tên chứa các Models (đối tượng dữ liệu) của ứng dụng
using BAOCAO_369.Models;

// Định nghĩa không gian tên cho ứng dụng (tương ứng với thư mục Controllers)
namespace BAOCAO_369.Controllers
{
    // Lớp BaoCaoController kế thừa từ Controller cơ sở của ASP.NET Core MVC
    public class BaoCaoController : Controller
    {
        // Khai báo biến nội bộ lưu trữ instance của BaoCaoService (Dịch vụ xử lý dữ liệu báo cáo)
        private readonly BaoCaoService _baoCaoService;
        // Khai báo biến nội bộ lưu trữ instance của ExcelExportService (Dịch vụ xuất file Excel)
        private readonly ExcelExportService _excelExportService;
        // Khai báo biến nội bộ lưu trữ instance của EmailService (Dịch vụ gửi mail)
        private readonly EmailService _emailService;

        // Phương thức khởi tạo (Constructor) của Controller, nhận Inject các Services cần thiết thông qua DI (Dependency Injection)
        public BaoCaoController(BaoCaoService baoCaoService, ExcelExportService excelExportService, EmailService emailService)
        {
            // Gán đối tượng baoCaoService được tiêm vào vào biến nội bộ của lớp
            _baoCaoService = baoCaoService;
            // Gán đối tượng excelExportService được tiêm vào vào biến nội bộ của lớp
            _excelExportService = excelExportService;
            // Gán đối tượng emailService được tiêm vào vào biến nội bộ của lớp
            _emailService = emailService;
        }

        // Action Index xử lý HTTP GET (mặc định), dùng để hiển thị giao diện chính.
        // Nhận vào các tham số tự động lấy từ query string: ngày bắt đầu (fromDate), ngày kết thúc (toDate), mã đơn vị (idDv)
        public IActionResult Index(DateTime? fromDate, DateTime? toDate, decimal? idDv)
        {
            // Bỏ kiểm tra quyền, mọi user đều có quyền thao tác
            ViewBag.IsAdmin = true;


            // Khởi tạo ngày bắt đầu mặc định là ngày mùng 1 tháng 1 của năm hiện tại
            var dateFrom = new DateTime(DateTime.Now.Year, 1, 1);
            // Khởi tạo ngày kết thúc mặc định là thời điểm hiện tại
            var dateToDefault = DateTime.Now;

            // Gọi phương thức GetBaoCaoItems từ baoCaoService để lấy danh sách báo cáo với ngày và mã đơn vị mặc định đó
            var items = _baoCaoService.GetBaoCaoItems(dateFrom, dateToDefault, idDv);
            // Gọi phương thức GetDonVis từ baoCaoService để lấy danh sách tất cả các đơn vị phục vụ cho Dropdown chọn đơn vị
            var donVis = _baoCaoService.GetDonVis();
            
            // Lấy ngày cập nhật mới nhất từ NGAY_TAO trong BC_VP_QD_296_BIEU_3
            // Chỉ xét các đơn vị đã có dữ liệu (NgayCapNhat != null)
            // Khởi tạo biến maxUpdateDate mặc định bằng giá trị thời gian hiện tại
            DateTime maxUpdateDate = dateToDefault;
            
            // Kiểm tra xem danh sách items trả về có tồn tại và không bị rỗng
            if (items != null && items.Count > 0)
            {
                // Lọc ra danh sách các ngày hợp lệ: Chỉ lấy các item có NgayCapNhat (có giá trị) rồi chuyển về kiểu DateTime
                var validDates = items
                    .Where(x => x.NgayCapNhat.HasValue) // Lọc các phần tử mà ngày cập nhật không null
                    .Select(x => x.NgayCapNhat.Value)   // Chỉ lấy ra giá trị Ngày
                    .ToList();                          // Chuyển kết quả về dạng List<DateTime>
                
                // Nếu danh sách ngày tồn tại (có ít nhất 1 dữ liệu cập nhật)
                if (validDates.Count > 0)
                    maxUpdateDate = validDates.Max();   // Gán maxUpdateDate bằng giá trị lớn nhất (ngày mới nhất) trong danh sách
            }

            // Tạo đối tượng ViewModel để gửi nhiều loại dữ liệu sang cho View sử dụng
            var viewModel = new BaoCaoViewModel
            {
                Items = items,          // Gán danh sách báo cáo
                DonVis = donVis,        // Gán danh sách đơn vị
                IdDv = idDv,            // Gán giá trị mã đơn vị hiện tại (nếu người dùng đang filter)
                FromDate = dateFrom,    // Gán giá trị ngày bắt đầu đã tính
                ToDate = maxUpdateDate  // Gán giá trị ngày lấy dữ liệu mới nhất (để hiển thị Last Updated)
            };

            // Trả về View kèm theo biến viewModel đã dựng sẵn chứa toàn bộ dữ liệu cần thiết của trang
            return View(viewModel);
        }


        // Khai báo Action ExportExcel để tải dữ liệu về máy dưới dạng file .xlsx
        public IActionResult ExportExcel(DateTime fromDate, DateTime toDate, decimal? idDv)
        {
            // Gán giá trị bắt đầu mặc định là ngày đầu tiên của năm hiện hành
            var dateFrom = new DateTime(DateTime.Now.Year, 1, 1);
            // Gán ngày kết thúc mặc định bằng thời gian hiện tại
            var dateToDefault = DateTime.Now;

            // Thực hiện query lấy mảng báo cáo từ database tương tự logic ở hàm Index
            var items = _baoCaoService.GetBaoCaoItems(dateFrom, dateToDefault, idDv);
            
            // Lấy ngày cập nhật mới nhất từ dữ liệu thực tế bằng cú pháp cấu trúc gọn của ngôn ngữ LINQ & Toán tử 3 ngôi (Ternary)
            // Nếu có bản ghi thì lấy Max của NgayCapNhat, nếu không tồn tại hoặc mảng rỗng thì gán mặc định dateToDefault
            var maxUpdateDate = items != null && items.Count > 0 
                ? items.Max(x => x.NgayCapNhat) ?? dateToDefault 
                : dateToDefault;

            // Lấy thêm chi tiết Cột 3 và Cột 8 để xuất vào các Sheet khác
            var detailsC3 = _baoCaoService.GetChiTietCot3(idDv ?? 0, dateFrom, maxUpdateDate);
            var detailsC8 = _baoCaoService.GetChiTietCot8(idDv ?? 0, dateFrom, maxUpdateDate);

            // Truyền dữ liệu vào Service thao tác với Excel để xuất sinh dữ liệu dạng byte array (3 Sheets)
            var fileContents = _excelExportService.ExportToExcel(items, detailsC3, detailsC8, dateFrom, maxUpdateDate);

            // Trả về file tải xuống cho client người dùng kèm theo byte data, kiểu mime Excel và đặt tên file tuỳ biến gồm ngày tháng
            return File(
                fileContents,                                                           // Mảng byte định nghĩa nội dung file Excel
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",    // Chuẩn định dạng MIME biểu diễn file Excel XLSX
                $"BaoCao_BieuMau3_{DateTime.Now:yyyyMMdd}.xlsx"                         // Nội suy ra tên file cùng chuỗi ngày theo định dạng YYYYMMDD
            );
        }

        // Khai báo Action ExportAllExcel để tải dữ liệu tất cả đơn vị thành 1 file .zip chứa nhiều file .xlsx
        public IActionResult ExportAllExcel(DateTime? fromDate, DateTime? toDate)
        {
            // Gán giá trị bắt đầu mặc định là ngày đầu tiên của năm hiện hành
            var dateFrom = fromDate ?? new DateTime(DateTime.Now.Year, 1, 1);
            // Gán ngày kết thúc mặc định bằng thời gian hiện tại
            var dateToDefault = toDate ?? DateTime.Now;

            // Lấy danh sách toàn bộ các đơn vị
            var donVis = _baoCaoService.GetDonVis();
            
            if (donVis == null || donVis.Count == 0)
            {
                return BadRequest("Không có dữ liệu đơn vị.");
            }

            // Tạo một MemoryStream để chứa nội dung file ZIP
            using (var compressedFileStream = new MemoryStream())
            {
                // Khởi tạo một ZipArchive để ghi dữ liệu vào MemoryStream
                using (var zipArchive = new ZipArchive(compressedFileStream, ZipArchiveMode.Create, true))
                {
                    // Lặp qua từng đơn vị để tạo báo cáo riêng biệt
                    foreach (var dv in donVis)
                    {
                        // Thực hiện query lấy mảng báo cáo cho RIÊNG ĐƠN VỊ ĐÓ
                        var items = _baoCaoService.GetBaoCaoItems(dateFrom, dateToDefault, dv.ID_DV);

                        // Nếu không có dữ liệu cho đơn vị này thì có thể bỏ qua hoặc vẫn xuất file trống tùy nhu cầu.
                        // Ở đây ta vẫn xuất file (thể hiện là không có dữ liệu)
                        var maxUpdateDate = items != null && items.Count > 0 
                            ? items.Max(x => x.NgayCapNhat) ?? dateToDefault 
                            : dateToDefault;

                        // Lấy thêm chi tiết Cột 3 và Cột 8 cho từng đơn vị
                        var detailsC3 = _baoCaoService.GetChiTietCot3(dv.ID_DV, dateFrom, maxUpdateDate);
                        var detailsC8 = _baoCaoService.GetChiTietCot8(dv.ID_DV, dateFrom, maxUpdateDate);

                        // Truyền dữ liệu vào Service thao tác với Excel để xuất sinh dữ liệu dạng byte array
                        var fileContents = _excelExportService.ExportToExcel(items, detailsC3, detailsC8, dateFrom, maxUpdateDate);

                        // Tạo tên file an toàn (bỏ các ký tự đặc biệt có thể lỗi path nếu có trong tên đơn vị)
                        string rawName = dv.TEN_DV ?? $"DonVi_{dv.ID_DV}";
                        var safeDonViName = string.Join("_", rawName.Split(Path.GetInvalidFileNameChars()));
                        var fileNameInZip = $"BaoCao_{safeDonViName}_{DateTime.Now:yyyyMMdd}.xlsx";

                        // Tạo một mục (entry) mới trong file ZIP
                        var zipEntry = zipArchive.CreateEntry(fileNameInZip, CompressionLevel.Fastest);

                        // Ghi byte array (nội dung file Excel) vào entry của ZIP
                        using (var originalFileStream = new MemoryStream(fileContents))
                        using (var zipEntryStream = zipEntry.Open())
                        {
                            originalFileStream.CopyTo(zipEntryStream);
                        }
                    }
                }

                // Chuyển con trỏ stream về đầu để trả về
                compressedFileStream.Position = 0;

                // Trả về file tải xuống cho client người dùng kèm theo byte data, kiểu mime ZIP và đặt tên file tuỳ biến
                return File(
                    compressedFileStream.ToArray(),
                    "application/zip",
                    $"BaoCao_TatCaDonVi_{DateTime.Now:yyyyMMdd}.zip"
                );
            }
        }
        // Khai báo Action History để xem lịch sử cập nhật dữ liệu của 1 đơn vị
        public IActionResult History(decimal idDv)
        {
            var donVis = _baoCaoService.GetDonVis();
            var donVi = donVis.FirstOrDefault(d => d.ID_DV == idDv);
            if (donVi == null)
            {
                return NotFound("Không tìm thấy đơn vị");
            }

            ViewBag.TenDonVi = donVi.TEN_DV;
            
            var items = _baoCaoService.GetLichSuDonVi(idDv);
            return View(items);
        }

        // Khai báo Action GetChiTietCot3 để lấy danh sách chi tiết hồ sơ khi click vào cột 3
        [HttpGet]
        public IActionResult GetChiTietCot3(decimal idDv, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var dateFrom = fromDate ?? new DateTime(DateTime.Now.Year, 1, 1);
                var dateTo = toDate ?? DateTime.Now;

                var items = _baoCaoService.GetChiTietCot3(idDv, dateFrom, dateTo);
                return Json(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Khai báo Action GetChiTietCot8 để lấy danh sách chi tiết hồ sơ khi click vào cột 8
        [HttpGet]
        public IActionResult GetChiTietCot8(decimal idDv, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var dateFrom = fromDate ?? new DateTime(DateTime.Now.Year, 1, 1);
                var dateTo = toDate ?? DateTime.Now;

                var items = _baoCaoService.GetChiTietCot8(idDv, dateFrom, dateTo);
                return Json(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class SendEmailRequest
        {
            public decimal IdDv { get; set; }
            public string ToEmail { get; set; }
            public string CcEmail { get; set; }
        }

        // --- Action gửi mail tới 1 đơn vị ---
        [HttpPost]
        public async Task<IActionResult> SendEmailToUnit([FromBody] SendEmailRequest req)
        {
            try
            {
                var donVis = _baoCaoService.GetDonVis();
                var donVi = donVis.FirstOrDefault(d => d.ID_DV == req.IdDv);
                
                if (donVi == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn vị." });
                }

                if (string.IsNullOrEmpty(req.ToEmail))
                {
                    return Json(new { success = false, message = "Vui lòng nhập địa chỉ Email người nhận." });
                }

                var dateFrom = new DateTime(DateTime.Now.Year, 1, 1);
                var items = _baoCaoService.GetBaoCaoItems(dateFrom, DateTime.Now, req.IdDv);

                if (items == null || items.Count == 0)
                {
                    return Json(new { success = false, message = "Đơn vị không có dữ liệu báo cáo để gửi." });
                }

                // Xuất file báo cáo hiện tại của đơn vị (3 sheets)
                var dateTo = items.Max(x => x.NgayCapNhat) ?? DateTime.Now;
                var detailsC3 = _baoCaoService.GetChiTietCot3(req.IdDv, dateFrom, dateTo);
                var detailsC8 = _baoCaoService.GetChiTietCot8(req.IdDv, dateFrom, dateTo);
                var excelBytes = _excelExportService.ExportToExcel(items, detailsC3, detailsC8, dateFrom, dateTo);

                var subject = $"[BÁO CÁO EVN] Báo cáo thống kê kết quả lập hồ sơ - {donVi.TEN_DV}";
                var body = $@"
                    <h3>Kính gửi: {donVi.TEN_DV}</h3>
                    <p>Hệ thống gửi Báo cáo thống kê kết quả lập hồ sơ tự động.</p>
                    <p>Ngày cập nhật số liệu gần nhất: <strong>{dateTo:dd/MM/yyyy HH:mm:ss}</strong></p>
                    <p>Vui lòng xem file Excel đính kèm để biết chi tiết.</p>
                    <br/>
                    <p><i>Lưu ý: Đây là email tự động, vui lòng không phản hồi.</i></p>
                ";

                var result = await _emailService.SendEmailWithAttachmentAsync(
                    req.ToEmail, subject, body, excelBytes, $"BaoCao_{donVi.MA_DV}_{DateTime.Now:yyyyMMdd}.xlsx", req.CcEmail);

                return Json(new { success = result.success, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // --- Action gửi mail tới tất cả các đơn vị ---
        [HttpPost]
        public async Task<IActionResult> SendAllEmails()
        {
            try
            {
                var donVis = _baoCaoService.GetDonVis().Where(x => !string.IsNullOrEmpty(x.EMAIL)).ToList();
                if (donVis.Count == 0)
                {
                    return Json(new { success = false, message = "Không có đơn vị nào được cấu hình Email." });
                }

                var dateFrom = new DateTime(DateTime.Now.Year, 1, 1);
                int successCount = 0;
                int failCount = 0;

                foreach (var donVi in donVis)
                {
                    var items = _baoCaoService.GetBaoCaoItems(dateFrom, DateTime.Now, donVi.ID_DV);
                    if (items == null || items.Count == 0) continue;

                    var dateTo = items.Max(x => x.NgayCapNhat) ?? DateTime.Now;
                    var detailsC3 = _baoCaoService.GetChiTietCot3(donVi.ID_DV, dateFrom, dateTo);
                    var detailsC8 = _baoCaoService.GetChiTietCot8(donVi.ID_DV, dateFrom, dateTo);
                    var excelBytes = _excelExportService.ExportToExcel(items, detailsC3, detailsC8, dateFrom, dateTo);

                    var subject = $"[BÁO CÁO EVN] Báo cáo thống kê kết quả lập hồ sơ - {donVi.TEN_DV}";
                    var body = $@"
                        <h3>Kính gửi: {donVi.TEN_DV}</h3>
                        <p>Hệ thống gửi Báo cáo thống kê kết quả lập hồ sơ tự động.</p>
                        <p>Vui lòng xem file Excel đính kèm để biết chi tiết.</p>
                        <br/>
                        <p><i>Lưu ý: Đây là email tự động, vui lòng không phản hồi.</i></p>
                    ";

                    var result = await _emailService.SendEmailWithAttachmentAsync(
                        donVi.EMAIL, subject, body, excelBytes, $"BaoCao_{donVi.MA_DV}_{DateTime.Now:yyyyMMdd}.xlsx");

                    if (result.success) successCount++;
                    else failCount++;
                }

                return Json(new { success = true, message = $"Hoàn tất! Gửi thành công: {successCount}, Thất bại: {failCount}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
