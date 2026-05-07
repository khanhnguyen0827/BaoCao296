// Import thư viện System cho các kiểu dữ liệu cơ bản
using System;
// Import thư viện Collection cho danh sách List
using System.Collections.Generic;
// Import thư viện xử lý luồng (Stream) dữ liệu file
using System.IO;
// Import thư viện ClosedXML.Excel chuyên thao tác xuất/nhập file Excel (.xlsx)
using ClosedXML.Excel;
// Import các models chứa dữ liệu báo cáo
using BAOCAO_369.Models;

// Định nghĩa không gian tên của các Service
namespace BAOCAO_369.Services
{
    // ==========================================
    // Dịch vụ chuyên trách hỗ trợ xuất dữ liệu ra file Excel
    // ==========================================
    public class ExcelExportService
    {
        // Hàm chính nhận vào list dữ liệu và thời gian, sau đó trả về 1 mảng byte (đại diện cho file Excel)
        public byte[] ExportToExcel(List<BaoCaoItem> items, DateTime fromDate, DateTime toDate)
        {
            // Khởi tạo một Workbook Excel mới trong bộ nhớ, tự động dọn dẹp khi dùng xong (using)
            using (var workbook = new XLWorkbook())
            {
                // Tạo một trang tính (Worksheet) mới với tên "BaoCao_BieuMau3"
                var worksheet = workbook.Worksheets.Add("BaoCao_BieuMau3");

                // --- 1. Tiêu đề Báo cáo (Hàng 1) ---
                // Ghi chữ vào ô A1 (Hàng 1, Cột 1)
                worksheet.Cell(1, 1).Value = "Biểu mẫu 3: THỐNG KÊ KẾT QUẢ LẬP HỒ SƠ & GIAO NỘP HỒ SƠ VÀO LƯU TRỮ HIỆN HÀNH";
                // Merge (Gộp) từ ô cột 1 đến cột 15 của hàng 1, In đậm và căn giữa nội dung
                worksheet.Range(1, 1, 1, 15).Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                
                // --- 2. Dòng Ngày (Hàng 2) ---
                // Ghi thời gian báo cáo vào ô A2 (Hàng 2, Cột 1) theo format dd/MM/yyyy
                worksheet.Cell(2, 1).Value = $"từ ngày {fromDate:dd/MM/yyyy} đến ngày {toDate:dd/MM/yyyy}";
                // Gộp từ cột 1-15 của hàng 2, sau đó căn giữa
                worksheet.Range(2, 1, 2, 15).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // --- 3. Tiêu đề Cột (Hàng 3, 4) ---
                // Khai báo một vùng tham chiếu (Range) từ dòng 3 cột 1 đến dòng 4 cột 15 để làm Header
                var headerRange = worksheet.Range(3, 1, 4, 15);
                // Định dạng Header: In đậm, căn giữa ngang và đứng, tự động xuống dòng (WrapText)
                headerRange.Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center).Alignment.SetWrapText(true);
                // Đóng khung (Border) viền ngoài mỏng, viền trong mỏng cho toàn bộ Header
                headerRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

                // Gộp ô theo chiều dọc cho từng Header và điền tiêu đề tương ứng cho cột 1 đến 15
                worksheet.Range(3, 1, 4, 1).Merge().Value = "TT";
                worksheet.Range(3, 2, 4, 2).Merge().Value = "Tên đơn vị";
                worksheet.Range(3, 3, 4, 3).Merge().Value = "Tổng số văn bản chủ trì chưa LHSCV";
                worksheet.Range(3, 4, 4, 4).Merge().Value = "Tổng số hồ sơ đang thực hiện";
                worksheet.Range(3, 5, 4, 5).Merge().Value = "Số lượng văn bản chủ trì có trong hồ sơ đang thực hiện";
                worksheet.Range(3, 6, 4, 6).Merge().Value = "Tổng số hồ sơ trả lại";
                worksheet.Range(3, 7, 4, 7).Merge().Value = "Số lượng văn bản chủ trì có trong hồ sơ trả lại";
                worksheet.Range(3, 8, 4, 8).Merge().Value = "Tổng số hồ sơ đã kết thúc, chưa giao nộp";
                worksheet.Range(3, 9, 4, 9).Merge().Value = "Số lượng văn bản chủ trì có trong hồ sơ đã kết thúc, chưa giao nộp";
                worksheet.Range(3, 10, 4, 10).Merge().Value = "Tổng số hồ sơ đang chờ tiếp nhận vào LTHH";
                worksheet.Range(3, 11, 4, 11).Merge().Value = "Số lượng VB chủ trì có trong hồ sơ chờ tiếp nhận vào LTHH";
                worksheet.Range(3, 12, 4, 12).Merge().Value = "HS đúng quy định đã được tiếp nhận vào LTHH";
                worksheet.Range(3, 13, 4, 13).Merge().Value = "Số lượng văn bản hồ sơ đã tiếp nhận vào LTHH";
                worksheet.Range(3, 14, 4, 14).Merge().Value = "Tổng số văn bản chủ trì giải quyết";
                worksheet.Range(3, 15, 4, 15).Merge().Value = "Tỷ lệ văn bản chủ trì (có trong hồ sơ đã tiếp nhận vào LTHH)/Tổng số văn bản được giao chủ trì";

                // --- 4. Đánh số thứ tự cột (Hàng 5) ---
                // Chạy vòng lặp từ 1 tới 15 để chèn số hiệu cột phía dưới tiêu đề
                for (int i = 1; i <= 15; i++)
                {
                    // Trỏ tới cell ở dòng 5 cột i
                    var cell = worksheet.Cell(5, i);
                    // Cột cuối hiển thị luôn công thức, các cột bình thường ghi "(số_thứ_tự)"
                    cell.Value = (i == 15) ? "(15)=(13)/(14)" : $"({i})";
                    // Căn giữa dòng số cột và In nghiêng (Italic)
                    cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetItalic();
                    
                    // Đổ màu Shading nền xám nhạt (#F2F2F2) cho các cột có chỉ số đánh dấu Cột 3, 5, 7, 9, 11, 13
                    if (i >= 3 && i % 2 != 0 && i < 15)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                    }
                }
                // Đồng thời set Border toàn bộ vòng của dòng số 5
                worksheet.Range(5, 1, 5, 15).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

                // --- 5. Đổ dữ liệu ---
                // Bắt đầu row dữ liệu từ dòng số 6
                int currentRow = 6;
                // Vòng lặp duyệt qua từng dòng dữ liệu báo cáo (item)
                foreach (var item in items)
                {
                    // Điền giá trị từ model item vào worksheet tương ứng cột từ 1 đến 15
                    worksheet.Cell(currentRow, 1).Value = item.TT;
                    worksheet.Cell(currentRow, 2).Value = item.TEN_DV;
                    worksheet.Cell(currentRow, 3).Value = item.COT_3;
                    worksheet.Cell(currentRow, 4).Value = item.COT_4;
                    worksheet.Cell(currentRow, 5).Value = item.COT_5;
                    worksheet.Cell(currentRow, 6).Value = item.COT_6;
                    worksheet.Cell(currentRow, 7).Value = item.COT_7;
                    worksheet.Cell(currentRow, 8).Value = item.COT_8;
                    worksheet.Cell(currentRow, 9).Value = item.COT_9;
                    worksheet.Cell(currentRow, 10).Value = item.COT_10;
                    worksheet.Cell(currentRow, 11).Value = item.COT_11;
                    worksheet.Cell(currentRow, 12).Value = item.COT_12;
                    worksheet.Cell(currentRow, 13).Value = item.COT_13;
                    worksheet.Cell(currentRow, 14).Value = item.COT_14;
                    // Cột 15 là giá trị phần trăm nên chia thẳng cho 100 để Excel tự Format kiểu %
                    worksheet.Cell(currentRow, 15).Value = item.COT_15 / 100; // formatted as percentage
                    
                    // Formatting & Shading (Định dạng & Đổ Bóng)
                    // Thiết lập viền xung quanh ô trên từng line dữ liệu
                    worksheet.Range(currentRow, 1, currentRow, 15).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    // Cài đặt Format number để cột 15 hiển thị dấu % và có 2 số thập phân ở Excel
                    worksheet.Cell(currentRow, 15).Style.NumberFormat.Format = "0.00%";
                    
                    // Đổ nền xám nhạt (#F2F2F2) lặp lại đều trên các ô ở các cột 3, 5, 7, 9, 11, 13
                    for (int i = 3; i <= 13; i += 2)
                    {
                        worksheet.Cell(currentRow, i).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                    }

                    // Xuống dòng tiếp theo
                    currentRow++;
                }

                // Tự động điều chỉnh bề rộng các cột theo độ dài nội dung (AutoFit)
                worksheet.Columns().AdjustToContents();
                // Override thiết lập cứng độ rộng cho các cột 3 đến 15 là 15, để khi nội dung dài thì WrapText sẽ kích hoạt đẹp mắt
                worksheet.Columns(3, 15).Width = 15; // Cố định độ rộng để Wrap Text hoạt động tốt

                // Tạo đối tượng MemoryStream lưu trữ tạm byte array vào bộ nhớ server trước khi gửi đi
                using (var stream = new MemoryStream())
                {
                    // Lưu file .xlsx vào stream
                    workbook.SaveAs(stream);
                    // Export byte array trả về function gọi phương thức
                    return stream.ToArray();
                }
            }
        }
    }
}
