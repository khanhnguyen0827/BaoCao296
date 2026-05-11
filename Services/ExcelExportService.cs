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
        public byte[] ExportToExcel(List<BaoCaoItem> items, List<ChiTietCot3Item> detailsC3, List<ChiTietCot8Item> detailsC8, DateTime fromDate, DateTime toDate)
        {
            // Khởi tạo một Workbook Excel mới trong bộ nhớ, tự động dọn dẹp khi dùng xong (using)
            using (var workbook = new XLWorkbook())
            {
                // ==========================================
                // SHEET 1: BÁO CÁO TỔNG HỢP
                // ==========================================
                var worksheet = workbook.Worksheets.Add("BaoCao_BieuMau3");

                // --- 1. Tiêu đề Báo cáo (Hàng 1) ---
                worksheet.Cell(1, 1).Value = "Biểu mẫu 3: THỐNG KÊ KẾT QUẢ LẬP HỒ SƠ & GIAO NỘP HỒ SƠ VÀO LƯU TRỮ HIỆN HÀNH";
                worksheet.Range(1, 1, 1, 15).Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                
                // --- 2. Dòng Ngày (Hàng 2) ---
                worksheet.Cell(2, 1).Value = $"từ ngày {fromDate:dd/MM/yyyy} đến ngày {toDate:dd/MM/yyyy}";
                worksheet.Range(2, 1, 2, 15).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // --- 3. Tiêu đề Cột (Hàng 3, 4) ---
                var headerRange = worksheet.Range(3, 1, 4, 15);
                headerRange.Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center).Alignment.SetWrapText(true);
                headerRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

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

                for (int i = 1; i <= 15; i++)
                {
                    var cell = worksheet.Cell(5, i);
                    cell.Value = (i == 15) ? "(15)=(13)/(14)" : $"({i})";
                    cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetItalic();
                    if (i >= 3 && i % 2 != 0 && i < 15) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                }
                worksheet.Range(5, 1, 5, 15).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

                int currentRow = 6;
                foreach (var item in items)
                {
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
                    worksheet.Cell(currentRow, 15).Value = item.COT_15 / 100;
                    
                    worksheet.Range(currentRow, 1, currentRow, 15).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    worksheet.Cell(currentRow, 15).Style.NumberFormat.Format = "0.00%";
                    for (int i = 3; i <= 13; i += 2) worksheet.Cell(currentRow, i).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                    currentRow++;
                }
                worksheet.Columns().AdjustToContents();
                worksheet.Columns(3, 15).Width = 15;

                // ==========================================
                // SHEET 2: CHI TIẾT CỘT 3
                // ==========================================
                var sheetC3 = workbook.Worksheets.Add("ChiTiet_Cot3");
                sheetC3.Cell(1, 1).Value = "CHI TIẾT VĂN BẢN CHỦ TRÌ CHƯA LẬP HỒ SƠ CÔNG VIỆC (CỘT 3)";
                sheetC3.Range(1, 1, 1, 8).Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var headerC3 = sheetC3.Range(2, 1, 2, 8);
                headerC3.Style.Font.SetBold().Fill.BackgroundColor = XLColor.LightBlue;
                headerC3.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

                sheetC3.Cell(2, 1).Value = "STT";
                sheetC3.Cell(2, 2).Value = "Đơn vị";
                sheetC3.Cell(2, 3).Value = "Phòng Ban";
                sheetC3.Cell(2, 4).Value = "Mã CV";
                sheetC3.Cell(2, 5).Value = "Ký Hiệu";
                sheetC3.Cell(2, 6).Value = "Hạn GQ";
                sheetC3.Cell(2, 7).Value = "Người Chủ Trì";
                sheetC3.Cell(2, 8).Value = "Trạng Thái";

                int rowC3 = 3;
                int sttC3 = 1;
                foreach (var detail in detailsC3)
                {
                    sheetC3.Cell(rowC3, 1).Value = sttC3++;
                    sheetC3.Cell(rowC3, 2).Value = detail.TEN_DV;
                    sheetC3.Cell(rowC3, 3).Value = detail.TEN_PB;
                    sheetC3.Cell(rowC3, 4).Value = detail.ID_CV;
                    sheetC3.Cell(rowC3, 5).Value = detail.KY_HIEU;
                    sheetC3.Cell(rowC3, 6).Value = detail.HAN_GQ;
                    sheetC3.Cell(rowC3, 7).Value = detail.FIRSTNAME;
                    sheetC3.Cell(rowC3, 8).Value = detail.TRANG_THAI_XU_LY;
                    sheetC3.Range(rowC3, 1, rowC3, 8).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    rowC3++;
                }
                sheetC3.Columns().AdjustToContents();

                // ==========================================
                // SHEET 3: CHI TIẾT CỘT 8
                // ==========================================
                var sheetC8 = workbook.Worksheets.Add("ChiTiet_Cot8");
                sheetC8.Cell(1, 1).Value = "CHI TIẾT HỒ SƠ ĐÃ KẾT THÚC CHƯA GIAO NỘP (CỘT 8)";
                sheetC8.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var headerC8 = sheetC8.Range(2, 1, 2, 7);
                headerC8.Style.Font.SetBold().Fill.BackgroundColor = XLColor.LightGreen;
                headerC8.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

                sheetC8.Cell(2, 1).Value = "STT";
                sheetC8.Cell(2, 2).Value = "Đơn vị";
                sheetC8.Cell(2, 3).Value = "Phòng Ban Lập HS";
                sheetC8.Cell(2, 4).Value = "Mã Hồ Sơ";
                sheetC8.Cell(2, 5).Value = "Tiêu Đề Hồ Sơ";
                sheetC8.Cell(2, 6).Value = "Năm";
                sheetC8.Cell(2, 7).Value = "Người Lập";

                int rowC8 = 3;
                int sttC8 = 1;
                foreach (var detail in detailsC8)
                {
                    sheetC8.Cell(rowC8, 1).Value = sttC8++;
                    sheetC8.Cell(rowC8, 2).Value = detail.TEN_DV;
                    sheetC8.Cell(rowC8, 3).Value = detail.PHONG_BAN_LAP_HS;
                    sheetC8.Cell(rowC8, 4).Value = detail.MA_HOSO;
                    sheetC8.Cell(rowC8, 5).Value = detail.TIEU_DE_HO_SO;
                    sheetC8.Cell(rowC8, 6).Value = detail.NAM_HS;
                    sheetC8.Cell(rowC8, 7).Value = detail.FIRSTNAME;
                    sheetC8.Range(rowC8, 1, rowC8, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
                    rowC8++;
                }
                sheetC8.Columns().AdjustToContents();

                // Tạo đối tượng MemoryStream lưu trữ tạm byte array vào bộ nhớ server trước khi gửi đi
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
