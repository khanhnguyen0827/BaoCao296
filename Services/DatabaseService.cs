using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace BAOCAO_369.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        public async Task<(int successCount, int errorCount, List<string> errors)> ImportDataAsync(string fileContent)
        {
            int successCount = 0;
            int errorCount = 0;
            List<string> errors = new List<string>();

            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                // 1. Cập nhật dữ liệu cũ disable = 1
                try
                {
                    string disableSql = "UPDATE BAOCAO.BC_VP_QD_296_BIEU_3 SET DISABLE = 1";
                    using (OracleCommand disableCmd = new OracleCommand(disableSql, conn))
                    {
                        await disableCmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Lỗi khi disable dữ liệu cũ: {ex.Message}");
                    errorCount++;
                }

                // 2. Phân tích nội dung file. Có thể là dạng lệnh SQL (;) hoặc dạng CSV có header
                var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(l => l.Trim())
                                       .Where(l => !string.IsNullOrEmpty(l))
                                       .ToList();

                if (lines.Count > 0 && lines[0].StartsWith("ID,ID_DV,COT_1"))
                {
                    // Dạng CSV
                    for (int i = 1; i < lines.Count; i++) // Bỏ qua dòng header
                    {
                        string line = lines[i];
                        string[] parts = line.Split(',');
                        if (parts.Length >= 18)
                        {
                            try
                            {
                                string insertSql = @"
                                    INSERT INTO BAOCAO.BC_VP_QD_296_BIEU_3 
                                    (ID_DV, COT_1, COT_2, COT_3, COT_4, COT_5, COT_6, COT_7, COT_8, COT_9, COT_10, COT_11, COT_12, COT_13, COT_14, DISABLE, NGAY_TAO) 
                                    VALUES 
                                    (:idDv, :c1, :c2, :c3, :c4, :c5, :c6, :c7, :c8, :c9, :c10, :c11, :c12, :c13, :c14, :disable, :ngayTao)";
                                
                                using (OracleCommand cmd = new OracleCommand(insertSql, conn))
                                {
                                    cmd.Parameters.Add(new OracleParameter("idDv", ParseDecimal(parts[1])));
                                    cmd.Parameters.Add(new OracleParameter("c1", ParseDecimal(parts[2])));
                                    cmd.Parameters.Add(new OracleParameter("c2", ParseDecimal(parts[3])));
                                    cmd.Parameters.Add(new OracleParameter("c3", ParseDecimal(parts[4])));
                                    cmd.Parameters.Add(new OracleParameter("c4", ParseDecimal(parts[5])));
                                    cmd.Parameters.Add(new OracleParameter("c5", ParseDecimal(parts[6])));
                                    cmd.Parameters.Add(new OracleParameter("c6", ParseDecimal(parts[7])));
                                    cmd.Parameters.Add(new OracleParameter("c7", ParseDecimal(parts[8])));
                                    cmd.Parameters.Add(new OracleParameter("c8", ParseDecimal(parts[9])));
                                    cmd.Parameters.Add(new OracleParameter("c9", ParseDecimal(parts[10])));
                                    cmd.Parameters.Add(new OracleParameter("c10", ParseDecimal(parts[11])));
                                    cmd.Parameters.Add(new OracleParameter("c11", ParseDecimal(parts[12])));
                                    cmd.Parameters.Add(new OracleParameter("c12", ParseDecimal(parts[13])));
                                    cmd.Parameters.Add(new OracleParameter("c13", ParseDecimal(parts[14])));
                                    cmd.Parameters.Add(new OracleParameter("c14", ParseDecimal(parts[15])));
                                    cmd.Parameters.Add(new OracleParameter("disable", ParseDecimal(parts[16])));
                                    
                                    var parsedDate = ParseDate(parts[17]);
                                    if (parsedDate.HasValue) {
                                        cmd.Parameters.Add(new OracleParameter("ngayTao", OracleDbType.Date) { Value = parsedDate.Value });
                                    } else {
                                        cmd.Parameters.Add(new OracleParameter("ngayTao", DBNull.Value));
                                    }
                                    
                                    await cmd.ExecuteNonQueryAsync();
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Lỗi dòng {i+1}: {ex.Message}");
                                errorCount++;
                            }
                        }
                    }
                }
                else if (lines.Count > 0 && lines[0].StartsWith("ID_DV,ID_PB_CHUTRI,PHONG_BAN_LAP_HS"))
                {
                    // Dạng CSV của Chi Tiết Cột 3
                    // Xóa dữ liệu cũ của bảng chi tiết trước khi import (tùy chọn, ở đây ta có thể dùng TRUNCATE hoặc DELETE ALL)
                    try
                    {
                        using (OracleCommand delCmd = new OracleCommand("DELETE FROM BAOCAO.BC_VP_QD_296_BIEU_3_CHITIET", conn))
                        {
                            await delCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Lỗi khi xóa dữ liệu cũ bảng chi tiết: {ex.Message}");
                    }

                    for (int i = 1; i < lines.Count; i++) // Bỏ qua dòng header
                    {
                        string line = lines[i];
                        // Xử lý split bằng dấu phẩy nhưng có thể bị dính dấu phẩy trong chuỗi có ngoặc kép. 
                        // Tuy nhiên với file đơn giản ta cứ Split(','). Cần cẩn thận nếu TIEU_DE_HO_SO có dấu phẩy.
                        // Hàm phân tích CSV thô sơ:
                        var parts = ParseCsvLine(line);
                        
                        if (parts.Count >= 9)
                        {
                            try
                            {
                                string insertSql = @"
                                    INSERT INTO BAOCAO.BC_VP_QD_296_BIEU_3_CHITIET 
                                    (ID_DV, ID_PB_CHUTRI, PHONG_BAN_LAP_HS, ID_HS, MA_HOSO, TIEU_DE_HO_SO, NAM_HS, NGUOI_LAP_HS, FIRSTNAME, NGAY_TAO) 
                                    VALUES 
                                    (:idDv, :idPbChuTri, :phongBanLapHs, :idHs, :maHoSo, :tieuDeHoSo, :namHs, :nguoiLapHs, :firstName, SYSDATE)";
                                
                                using (OracleCommand cmd = new OracleCommand(insertSql, conn))
                                {
                                    cmd.Parameters.Add(new OracleParameter("idDv", ParseDecimal(parts[0])));
                                    cmd.Parameters.Add(new OracleParameter("idPbChuTri", ParseDecimal(parts[1])));
                                    cmd.Parameters.Add(new OracleParameter("phongBanLapHs", string.IsNullOrEmpty(parts[2]) ? (object)DBNull.Value : parts[2]));
                                    cmd.Parameters.Add(new OracleParameter("idHs", ParseDecimal(parts[3])));
                                    cmd.Parameters.Add(new OracleParameter("maHoSo", string.IsNullOrEmpty(parts[4]) ? (object)DBNull.Value : parts[4]));
                                    
                                    // Xử lý cắt chuỗi nếu độ dài quá lớn so với NVARCHAR2
                                    string tieuDe = parts[5];
                                    if (tieuDe != null && tieuDe.Length > 900) tieuDe = tieuDe.Substring(0, 900);
                                    cmd.Parameters.Add(new OracleParameter("tieuDeHoSo", string.IsNullOrEmpty(tieuDe) ? (object)DBNull.Value : tieuDe));
                                    
                                    cmd.Parameters.Add(new OracleParameter("namHs", ParseDecimal(parts[6])));
                                    cmd.Parameters.Add(new OracleParameter("nguoiLapHs", ParseDecimal(parts[7])));
                                    cmd.Parameters.Add(new OracleParameter("firstName", string.IsNullOrEmpty(parts[8]) ? (object)DBNull.Value : parts[8]));
                                    
                                    await cmd.ExecuteNonQueryAsync();
                                    successCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Lỗi dòng {i+1}: {ex.Message}");
                                errorCount++;
                            }
                        }
                    }
                }
                else
                {
                    // Dạng kịch bản SQL thuần túy tách bằng dấu ;
                    var statements = fileContent.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(s => s.Trim())
                                               .Where(s => !string.IsNullOrEmpty(s))
                                               .ToList();

                    foreach (var stmt in statements)
                    {
                        try
                        {
                            using (OracleCommand cmd = new OracleCommand(stmt, conn))
                            {
                                await cmd.ExecuteNonQueryAsync();
                                successCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Lỗi lệnh: {stmt.Substring(0, Math.Min(stmt.Length, 30))}... Chi tiết: {ex.Message}");
                            errorCount++;
                        }
                    }
                }
            }

            return (successCount, errorCount, errors);
        }

        public async Task<(int successCount, int errorCount, List<string> errors)> ImportExcelDataAsync(Stream excelStream, decimal? targetIdDv = null)
        {
            int successCount = 0;
            int errorCount = 0;
            List<string> errors = new List<string>();

            try
            {
                using (var workbook = new XLWorkbook(excelStream))
                {
                    using (OracleConnection conn = new OracleConnection(_connectionString))
                    {
                        await conn.OpenAsync();

                        // --- SHEET 1: BÁO CÁO TỔNG (GRID 1) ---
                        var sheet1 = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("1") || w.Position == 1);
                        if (sheet1 != null)
                        {
                            var rows = sheet1.RowsUsed().Skip(1).ToList(); // Bỏ header
                            
                            // Xác định danh sách các Đơn vị cần Update (Nếu có targetIdDv thì chỉ lấy đúng đơn vị đó, nếu không thì lấy danh sách Unique từ file)
                            var idsToUpdate = new List<decimal>();
                            if (targetIdDv.HasValue)
                            {
                                idsToUpdate.Add(targetIdDv.Value);
                            }
                            else
                            {
                                idsToUpdate = rows.Select(r => ParseDecimal(r.Cell(2).Value.ToString()))
                                                  .Where(v => v.HasValue)
                                                  .Select(v => v.Value)
                                                  .Distinct().ToList();
                            }

                            // Disable dữ liệu cũ CHỈ CHO các đơn vị có trong danh sách cập nhật
                            foreach (var id in idsToUpdate)
                            {
                                string disableSql = "UPDATE BAOCAO.BC_VP_QD_296_BIEU_3 SET DISABLE = 1 WHERE ID_DV = :idDv";
                                using (OracleCommand disableCmd = new OracleCommand(disableSql, conn))
                                {
                                    disableCmd.Parameters.Add(new OracleParameter("idDv", id));
                                    await disableCmd.ExecuteNonQueryAsync();
                                }
                            }

                            foreach (var row in rows)
                            {
                                decimal? rowIdDv = ParseDecimal(row.Cell(2).Value.ToString());
                                // Nếu đang ở chế độ Import riêng cho 1 Đơn vị, bỏ qua các dòng không thuộc đơn vị đó
                                if (targetIdDv.HasValue && rowIdDv != targetIdDv.Value) continue;
                                
                                try
                                {
                                    string insertSql = @"
                                        INSERT INTO BAOCAO.BC_VP_QD_296_BIEU_3 
                                        (ID_DV, COT_3, COT_4, COT_5, COT_6, COT_7, COT_8, COT_9, COT_10, COT_11, COT_12, COT_13, COT_14, DISABLE, NGAY_TAO) 
                                        VALUES 
                                        (:idDv, :c3, :c4, :c5, :c6, :c7, :c8, :c9, :c10, :c11, :c12, :c13, :c14, 0, SYSDATE)";
                                    
                                    using (OracleCommand cmd = new OracleCommand(insertSql, conn))
                                    {
                                        cmd.Parameters.Add(new OracleParameter("idDv", ParseDecimal(row.Cell(2).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c3", ParseDecimal(row.Cell(5).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c4", ParseDecimal(row.Cell(6).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c5", ParseDecimal(row.Cell(7).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c6", ParseDecimal(row.Cell(8).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c7", ParseDecimal(row.Cell(9).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c8", ParseDecimal(row.Cell(10).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c9", ParseDecimal(row.Cell(11).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c10", ParseDecimal(row.Cell(12).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c11", ParseDecimal(row.Cell(13).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c12", ParseDecimal(row.Cell(14).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c13", ParseDecimal(row.Cell(15).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("c14", ParseDecimal(row.Cell(16).Value.ToString())));
                                        
                                        await cmd.ExecuteNonQueryAsync();
                                        successCount++;
                                    }
                                }
                                catch (Exception ex) { errors.Add($"Sheet 1 - Lỗi dòng {row.RowNumber()}: {ex.Message}"); errorCount++; }
                            }
                        }

                        // --- SHEET 2: CHI TIẾT CỘT 3 (GRID 2) ---
                        var sheet2 = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("2") || w.Position == 2);
                        if (sheet2 != null)
                        {
                            var rows = sheet2.RowsUsed().Skip(1).ToList();
                            
                            // Xác định danh sách các Đơn vị cần Update
                            var idsToUpdate = new List<decimal>();
                            if (targetIdDv.HasValue)
                            {
                                idsToUpdate.Add(targetIdDv.Value);
                            }
                            else
                            {
                                idsToUpdate = rows.Select(r => ParseDecimal(r.Cell(1).Value.ToString()))
                                                  .Where(v => v.HasValue)
                                                  .Select(v => v.Value)
                                                  .Distinct().ToList();
                            }

                            // Disable dữ liệu cũ CHỈ CHO các đơn vị có trong danh sách cập nhật
                            foreach (var id in idsToUpdate)
                            {
                                string disableSql = "UPDATE BAOCAO.BC_VP_QD_296_BIEU_3_CHITIET_C3 SET DISABLE = 1 WHERE ID_DV = :idDv";
                                using (OracleCommand disableCmd = new OracleCommand(disableSql, conn))
                                {
                                    disableCmd.Parameters.Add(new OracleParameter("idDv", id));
                                    await disableCmd.ExecuteNonQueryAsync();
                                }
                            }

                            foreach (var row in rows)
                            {
                                decimal? rowIdDv = ParseDecimal(row.Cell(1).Value.ToString());
                                if (targetIdDv.HasValue && rowIdDv != targetIdDv.Value) continue;
                                
                                try
                                {
                                    string insertSql = @"
                                        INSERT INTO BAOCAO.BC_VP_QD_296_BIEU_3_CHITIET_C3 
                                        (ID_DV, TEN_DV, ID_PB_CHUTRI, TEN_PB, ID_CV, KY_HIEU, HAN_GQ, NGUOI_CHU_TRI, FIRSTNAME, TRANG_THAI_XU_LY, LAP_HSCV_NV, DISABLE, NGAY_TAO) 
                                        VALUES 
                                        (:idDv, :tenDv, :idPb, :tenPb, :idCv, :kyHieu, :hanGq, :nguoiCt, :fname, :status, :lapHscv, 0, SYSDATE)";
                                    
                                    using (OracleCommand cmd = new OracleCommand(insertSql, conn))
                                    {
                                        cmd.Parameters.Add(new OracleParameter("idDv", ParseDecimal(row.Cell(1).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("tenDv", row.Cell(2).Value.ToString()));
                                        cmd.Parameters.Add(new OracleParameter("idPb", ParseDecimal(row.Cell(3).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("tenPb", row.Cell(4).Value.ToString()));
                                        cmd.Parameters.Add(new OracleParameter("idCv", ParseDecimal(row.Cell(5).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("kyHieu", row.Cell(6).Value.ToString()));
                                        
                                        var hanGq = row.Cell(7).Value.ToString();
                                        cmd.Parameters.Add(new OracleParameter("hanGq", ParseDate(hanGq) ?? (object)DBNull.Value));
                                        
                                        cmd.Parameters.Add(new OracleParameter("nguoiCt", ParseDecimal(row.Cell(8).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("fname", row.Cell(9).Value.ToString()));
                                        cmd.Parameters.Add(new OracleParameter("status", row.Cell(10).Value.ToString()));
                                        cmd.Parameters.Add(new OracleParameter("lapHscv", ParseDecimal(row.Cell(11).Value.ToString())));
                                        
                                        await cmd.ExecuteNonQueryAsync();
                                        successCount++;
                                    }
                                }
                                catch (Exception ex) { errors.Add($"Sheet 2 - Lỗi dòng {row.RowNumber()}: {ex.Message}"); errorCount++; }
                            }
                        }

                        // --- SHEET 3: CHI TIẾT CỘT 8 (GRID 3) ---
                        var sheet3 = workbook.Worksheets.FirstOrDefault(w => w.Name.Contains("3") || w.Position == 3);
                        if (sheet3 != null)
                        {
                            var rows = sheet3.RowsUsed().Skip(1).ToList();
                            
                            // Xác định danh sách các Đơn vị cần Update
                            var idsToUpdate = new List<decimal>();
                            if (targetIdDv.HasValue)
                            {
                                idsToUpdate.Add(targetIdDv.Value);
                            }
                            else
                            {
                                idsToUpdate = rows.Select(r => ParseDecimal(r.Cell(1).Value.ToString()))
                                                  .Where(v => v.HasValue)
                                                  .Select(v => v.Value)
                                                  .Distinct().ToList();
                            }

                            // Disable dữ liệu cũ CHỈ CHO các đơn vị có trong danh sách cập nhật
                            foreach (var id in idsToUpdate)
                            {
                                string disableSql = "UPDATE BAOCAO.BC_VP_QD_296_BIEU_3_CHITIET_C8 SET DISABLE = 1 WHERE ID_DV = :idDv";
                                using (OracleCommand disableCmd = new OracleCommand(disableSql, conn))
                                {
                                    disableCmd.Parameters.Add(new OracleParameter("idDv", id));
                                    await disableCmd.ExecuteNonQueryAsync();
                                }
                            }

                            foreach (var row in rows)
                            {
                                decimal? rowIdDv = ParseDecimal(row.Cell(1).Value.ToString());
                                if (targetIdDv.HasValue && rowIdDv != targetIdDv.Value) continue;
                                
                                try
                                {
                                    string insertSql = @"
                                        INSERT INTO BAOCAO.BC_VP_QD_296_BIEU_3_CHITIET_C8 
                                        (ID_DV, ID_PB_CHUTRI, PHONG_BAN_LAP_HS, ID_HS, MA_HOSO, TIEU_DE_HO_SO, NAM_HS, NGUOI_LAP_HS, FIRSTNAME, DISABLE, NGAY_TAO) 
                                        VALUES 
                                        (:idDv, :idPb, :tenPb, :idHs, :maHs, :tieuDe, :namHs, :nguoiLap, :fname, 0, SYSDATE)";
                                    
                                    using (OracleCommand cmd = new OracleCommand(insertSql, conn))
                                    {
                                        cmd.Parameters.Add(new OracleParameter("idDv", ParseDecimal(row.Cell(1).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("idPb", ParseDecimal(row.Cell(2).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("tenPb", row.Cell(3).Value.ToString()));
                                        cmd.Parameters.Add(new OracleParameter("idHs", ParseDecimal(row.Cell(4).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("maHs", row.Cell(5).Value.ToString()));
                                        cmd.Parameters.Add(new OracleParameter("tieuDe", row.Cell(6).Value.ToString()));
                                        cmd.Parameters.Add(new OracleParameter("namHs", ParseDecimal(row.Cell(7).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("nguoiLap", ParseDecimal(row.Cell(8).Value.ToString())));
                                        cmd.Parameters.Add(new OracleParameter("fname", row.Cell(9).Value.ToString()));
                                        
                                        await cmd.ExecuteNonQueryAsync();
                                        successCount++;
                                    }
                                }
                                catch (Exception ex) { errors.Add($"Sheet 3 - Lỗi dòng {row.RowNumber()}: {ex.Message}"); errorCount++; }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Lỗi chung: {ex.Message}");
                errorCount++;
            }

            return (successCount, errorCount, errors);
        }

        // Hàm hỗ trợ parse CSV đơn giản xử lý được dấu phẩy nằm trong ngoặc kép
        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentToken = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentToken.ToString().Trim('\"', ' '));
                    currentToken.Clear();
                }
                else
                {
                    currentToken.Append(c);
                }
            }
            result.Add(currentToken.ToString().Trim('\"', ' '));
            return result;
        }

        private decimal? ParseDecimal(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            if (decimal.TryParse(val, out decimal result)) return result;
            return null;
        }

        private DateTime? ParseDate(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            if (DateTime.TryParseExact(val, "dd-MMM-yy", new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out DateTime dt))
                return dt;
            if (DateTime.TryParse(val, out DateTime dt2))
                return dt2;
            return null;
        }
    }
}
